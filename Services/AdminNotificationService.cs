using System.Linq.Expressions;
using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using FlomiApp.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AdminNotificationService : IAdminNotificationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IMailQueue _mailQueue;
    private readonly ILogger<AdminNotificationService> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly NavigationManager? _navigation;

    public AdminNotificationService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IMailQueue mailQueue,
        ILogger<AdminNotificationService> logger,
        IHttpContextAccessor? httpContextAccessor = null,
        NavigationManager? navigation = null)
    {
        _dbFactory           = dbFactory;
        _mailQueue           = mailQueue;
        _logger              = logger;
        _httpContextAccessor = httpContextAccessor;
        _navigation          = navigation;
    }

    public async Task NotifyNewUserAsync(string userId, string method)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var recipients = await RecipientsAsync(db, u => u.NotifyNewUsers);
            if (recipients.Count == 0) return;

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return;

            var mail = new AdminNewUserMail(
                FullName:       $"{user.FirstName} {user.LastName}".Trim(),
                Pfadiname:      user.Pfadiname,
                Stufe:          user.Stufe,
                Birthday:       user.Birthday,
                Email:          user.Email ?? "",
                EmailConfirmed: user.EmailConfirmed,
                PhoneNumber:    user.PhoneNumber,
                Method:         method,
                CreatedAtUtc:   DateTime.UtcNow,
                TotalUsers:     await db.Users.CountAsync(),
                AdminUrl:       Link("/admin/users"));

            foreach (var to in recipients)
                _mailQueue.Enqueue(m => m.SendAdminNewUserAsync(to, mail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin-Benachrichtigung zu neuem Konto {UserId} fehlgeschlagen", userId);
        }
    }

    public async Task NotifyRegistrationAsync(int appointmentId, bool cancelled = false)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var recipients = await RecipientsAsync(db, u => u.NotifyRegistrations);
            if (recipients.Count == 0) return;

            var apt = await db.Appointments.AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.FamilyMember)
                .Include(a => a.Area).ThenInclude(a => a.Event)
                .Include(a => a.Area).ThenInclude(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (apt == null) return;

            var area       = apt.Area;
            var registered = await db.Appointments.CountAsync(a =>
                a.AreaId == area.Id
                && a.Status == AppointmentStatus.Registered
                && a.UseAlternativeSlot == apt.UseAlternativeSlot);
            var capacity = apt.UseAlternativeSlot && area.AlternativeMaxCapacity.HasValue
                ? area.AlternativeMaxCapacity.Value
                : area.MaxCapacity;
            var inEvent = await db.Appointments.CountAsync(a =>
                a.UserId == apt.UserId
                && a.Status == AppointmentStatus.Registered
                && a.Area.EventId == area.EventId);

            var member      = apt.FamilyMember;
            var accountName = $"{apt.User.FirstName} {apt.User.LastName}".Trim();

            var mail = new AdminRegistrationMail(
                Cancelled:       cancelled,
                PersonName:      member != null ? $"{member.FirstName} {member.LastName}".Trim() : accountName,
                PersonStufe:     member != null ? member.Stufe : apt.User.Stufe,
                PersonBirthday:  member != null ? member.Birthday : apt.User.Birthday,
                ForFamilyMember: member != null,
                AccountName:     accountName,
                AccountEmail:    apt.User.Email ?? "",
                AccountPhone:    apt.User.PhoneNumber,
                EventName:       area.Event?.Name ?? "",
                AreaName:        area.AreaTemplate?.Name ?? "",
                CategoryName:    area.AreaTemplate?.AreaCategory?.Name,
                Date:            AreaSchedule.Date(area),
                TimeSlot:        AreaSchedule.TimeSlot(area, apt.UseAlternativeSlot),
                Comment:         apt.Comment,
                Registered:      registered,
                Capacity:        capacity,
                AccountRegistrationsInEvent: inEvent,
                AtUtc:           DateTime.UtcNow,
                AdminUrl:        Link("/admin/appointments"));

            foreach (var to in recipients)
                _mailQueue.Enqueue(m => m.SendAdminRegistrationAsync(to, mail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin-Benachrichtigung zu Anmeldung {AppointmentId} fehlgeschlagen", appointmentId);
        }
    }

    // Empfänger: Konten mit Rolle «Admin», bestätigter E-Mail und eingeschalteter Benachrichtigung
    private static Task<List<string>> RecipientsAsync(
        ApplicationDbContext db, Expression<Func<ApplicationUser, bool>> wants) =>
        (from u in db.Users.Where(wants)
         join ur in db.UserRoles on u.Id equals ur.UserId
         join r in db.Roles on ur.RoleId equals r.Id
         where r.Name == Roles.Admin && u.Email != null && u.EmailConfirmed
         select u.Email!)
        .Distinct()
        .ToListAsync();

    // Absolute Adresse für den Link in der Mail: aus der Anfrage (Razor Pages) oder aus der
    // Blazor-Verbindung. Ohne beides gibt es einfach keinen Link.
    private string? Link(string path)
    {
        var request = _httpContextAccessor?.HttpContext?.Request;
        if (request is { Host.HasValue: true })
            return $"{request.Scheme}://{request.Host}{path}";

        try
        {
            return _navigation != null ? _navigation.BaseUri.TrimEnd('/') + path : null;
        }
        catch (InvalidOperationException)
        {
            return null; // NavigationManager ausserhalb einer Blazor-Verbindung
        }
    }
}
