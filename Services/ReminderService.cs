using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class ReminderService : IReminderService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IMailQueue _mailQueue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IMailQueue mailQueue,
        IConfiguration configuration,
        ILogger<ReminderService> logger)
    {
        _dbFactory     = dbFactory;
        _mailQueue     = mailQueue;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<int> SendDueRemindersAsync(DateTime utcNow)
    {
        if (ReminderSchedule.DueAreaDate(utcNow) is not { } date) return 0;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var due = await AppointmentsOn(db, date)
            .Where(a => a.ReminderSentAt == null)
            .ToListAsync();
        if (due.Count == 0) return 0;

        var vehicles = await VehicleDatesAsync(db, date);
        var sent = 0;

        foreach (var group in due.GroupBy(a => a.UserId))
        {
            // Auch ohne Mail als erledigt markieren, damit die Abfrage nicht jedes Mal dieselben findet
            foreach (var apt in group) apt.ReminderSentAt = utcNow;

            var user = group.First().User;
            if (!user.EmailNotificationsEnabled || string.IsNullOrEmpty(user.Email)) continue;

            var mail  = BuildMail(user, date, group, vehicles);
            var email = user.Email;
            _mailQueue.Enqueue(m => m.SendDayBeforeReminderAsync(email, $"{user.FirstName} {user.LastName}", mail));
            sent++;
        }

        await db.SaveChangesAsync();
        if (sent > 0) _logger.LogInformation("Erinnerungen für {Date:dd.MM.yyyy}: {Count} Mail(s) eingereiht", date, sent);
        return sent;
    }

    public async Task<bool> SendTestReminderAsync(string userId, int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return false;

        // Eigene Einsätze im Event (frühester Tag), sonst ein Bereich des Events als Beispiel
        var own = await WithDetails(db.Appointments.AsNoTracking())
            .Where(a => a.UserId == userId && a.Status == AppointmentStatus.Registered && a.Area.EventId == eventId)
            .ToListAsync();

        List<Appointment> items;
        if (own.Count > 0)
        {
            var firstDay = own.Min(a => a.Area.Date.Date);
            items = own.Where(a => a.Area.Date.Date == firstDay).ToList();
        }
        else
        {
            var area = await db.Areas.AsNoTracking()
                .Include(a => a.Event)
                .Include(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory)
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.IsSpontaneous).ThenBy(a => a.Date)
                .FirstOrDefaultAsync();
            if (area == null) return false;
            items = new() { new Appointment { Id = 0, UserId = userId, User = user, Area = area, AreaId = area.Id } };
        }

        var date     = items[0].Area.Date.Date;
        var vehicles = await VehicleDatesAsync(db, date);
        var mail     = BuildMail(user, date, items, vehicles) with { IsTest = true };
        var email    = user.Email;
        _mailQueue.Enqueue(m => m.SendDayBeforeReminderAsync(email, $"{user.FirstName} {user.LastName}", mail));
        return true;
    }

    public async Task<ReminderStatus> GetTomorrowStatusAsync(DateTime utcNow)
    {
        var swissNow = TimeHelper.ToSwissTime(utcNow);
        var date     = swissNow.Date.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await AppointmentsOn(db, date)
            .Select(a => new { a.UserId, Reminded = a.ReminderSentAt != null })
            .ToListAsync();

        return new ReminderStatus(
            Date:           date,
            Appointments:   rows.Count,
            People:         rows.Select(r => r.UserId).Distinct().Count(),
            PeopleReminded: rows.Where(r => r.Reminded).Select(r => r.UserId).Distinct().Count(),
            SendTimePassed: swissNow.TimeOfDay >= ReminderSchedule.SendTime);
    }

    // Aktive Anmeldungen an einem Tag, nur Events mit eingeschalteten Erinnerungen und ohne Beispieldaten
    private static IQueryable<Appointment> AppointmentsOn(ApplicationDbContext db, DateTime date) =>
        WithDetails(db.Appointments)
            .Where(a => a.Status == AppointmentStatus.Registered
                     && a.Area.Date >= date && a.Area.Date < date.AddDays(1)
                     && a.Area.Event!.RemindersEnabled
                     && !a.Area.Event.IsSample);

    private static IQueryable<Appointment> WithDetails(IQueryable<Appointment> query) =>
        query
            .Include(a => a.User)
            .Include(a => a.FamilyMember)
            .Include(a => a.Area).ThenInclude(a => a.Event)
            .Include(a => a.Area).ThenInclude(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory);

    private sealed record VehicleDay(AssignmentDate Day, string? DriverName, string? HelperName);

    // Einsatzplan des Tages; Fahrer und Beifahrer mit Konto stehen nur als Id drin, Namen hier auflösen
    private static async Task<List<VehicleDay>> VehicleDatesAsync(ApplicationDbContext db, DateTime date)
    {
        var days = await db.AssignmentDates.AsNoTracking()
            .Include(d => d.Assignment).ThenInclude(a => a.Vehicle)
            .Where(d => d.Date >= date && d.Date < date.AddDays(1))
            .ToListAsync();

        var ids = days.SelectMany(d => new[] { d.DriverUserId, d.HelperUserId })
            .Where(id => id != null).Distinct().ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

        return days.Select(d => new VehicleDay(d,
                d.DriverName ?? (d.DriverUserId != null ? names.GetValueOrDefault(d.DriverUserId) : null),
                d.HelperName ?? (d.HelperUserId != null ? names.GetValueOrDefault(d.HelperUserId) : null)))
            .ToList();
    }

    private ReminderMail BuildMail(ApplicationUser user, DateTime date, IEnumerable<Appointment> appointments,
        List<VehicleDay> vehicles)
    {
        var list = appointments.ToList();
        var items = list
            .OrderBy(a => a.Area.IsSpontaneous)
            .ThenBy(a => AreaSchedule.TimeSlot(a.Area, a.UseAlternativeSlot))
            .ThenBy(a => a.Area.AreaTemplate?.Name)
            .Select(a => new ReminderItem(
                AppointmentId: a.Id,
                AreaName:      a.Area.AreaTemplate?.Name ?? "",
                CategoryName:  a.Area.AreaTemplate?.AreaCategory?.Name,
                TimeSlot:      AreaSchedule.TimeSlot(a.Area, a.UseAlternativeSlot),
                IsSpontaneous: a.Area.IsSpontaneous,
                Location:      a.Area.AreaTemplate?.Location,
                ForPerson:     a.FamilyMember != null ? $"{a.FamilyMember.FirstName} {a.FamilyMember.LastName}".Trim() : null,
                Comment:       a.Comment,
                VehicleInfo:   a.FamilyMember == null ? VehicleInfo(user.Id, a.Area.EventId, vehicles) : null,
                WithQrCode:    a.Id > 0
                               && a.Area.Event?.CheckInEnabled == true
                               && a.Area.AreaTemplate?.AreaCategory?.Name == "Verkauf"))
            .ToList();

        var eventName = string.Join(" / ", list.Select(a => a.Area.Event?.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct());
        var baseUrl   = _configuration["App:PublicUrl"]?.TrimEnd('/');
        return new ReminderMail(user.FirstName, date, eventName, items,
            string.IsNullOrEmpty(baseUrl) ? null : baseUrl + "/user/dashboard");
    }

    // Fahrer/Beifahrer: Fahrzeug aus dem Einsatzplan und wer mitfährt
    private static string? VehicleInfo(string userId, int eventId, List<VehicleDay> vehicles)
    {
        var entry = vehicles.FirstOrDefault(v => v.Day.Assignment.EventId == eventId
                                                 && (v.Day.DriverUserId == userId || v.Day.HelperUserId == userId));
        if (entry == null) return null;
        var day = entry.Day;

        var owner   = day.Assignment.Vehicle?.OwnerName;
        var vehicle = string.IsNullOrWhiteSpace(owner) ? "Fahrzeug eingeteilt" : $"Fahrzeug von {owner}";
        var partner = day.DriverUserId == userId
            ? (entry.HelperName is { } helper ? $"Beifahrer: {helper}" : "noch ohne Beifahrer")
            : (entry.DriverName is { } driver ? $"Fahrer: {driver}" : "noch ohne Fahrer");
        return $"{vehicle} · {partner}";
    }
}
