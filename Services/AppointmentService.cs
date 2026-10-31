using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAreaService _areaService;
    private readonly IMailQueue _mailQueue;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        ApplicationDbContext context,
        IAreaService areaService,
        IMailQueue mailQueue,
        IAdminNotificationService adminNotifications,
        ILogger<AppointmentService> logger)
    {
        _context            = context;
        _areaService        = areaService;
        _mailQueue          = mailQueue;
        _adminNotifications = adminNotifications;
        _logger             = logger;
    }

    public async Task<List<Appointment>> GetUserAppointmentsAsync(string userId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(area => area.Event)
            .Include(a => a.Area)
                .ThenInclude(area => area.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .Include(a => a.User)
            .Include(a => a.FamilyMember)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Area.Date)
            .ToListAsync();
    }

    public async Task<List<FamilyMember>> GetFamilyMembersAsync(string userId)
    {
        return await _context.FamilyMembers
            .Where(f => f.UserId == userId)
            .ToListAsync();
    }

    public async Task AddFamilyMemberAsync(string userId, FamilyMember familyMember)
    {
        familyMember.UserId = userId;
        _context.FamilyMembers.Add(familyMember);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateFamilyMemberAsync(int familyMemberId, string userId, FamilyMember familyMember)
    {
        var existingMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(f => f.Id == familyMemberId && f.UserId == userId);

        if (existingMember == null)
            throw new InvalidOperationException("Family member not found.");

        existingMember.FirstName = familyMember.FirstName;
        existingMember.LastName  = familyMember.LastName;
        existingMember.Pfadiname = familyMember.Pfadiname;
        existingMember.Stufe     = familyMember.Stufe;
        existingMember.Birthday  = familyMember.Birthday;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteFamilyMemberAsync(int familyMemberId, string userId)
    {
        var familyMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(f => f.Id == familyMemberId && f.UserId == userId);

        if (familyMember != null)
        {
            _context.FamilyMembers.Remove(familyMember);
            await _context.SaveChangesAsync();
        }
    }

    private bool IsVerkauf(Area area)
        => area.AreaTemplate?.AreaCategory?.Name == "Verkauf";

    private bool IsKuchen(Area area)
        => area.AreaTemplate?.AreaCategory?.Name == "Kuchen";

    public async Task RegisterForAppointmentAsync(string userId, int areaId, int? familyMemberId = null, string? comment = null, bool useAlternativeSlot = false)
    {
        var check = await CheckRegistrationAsync(userId, areaId, familyMemberId, useAlternativeSlot);
        if (!check.CanRegister)
        {
            var firstName = familyMemberId.HasValue
                ? (await _context.FamilyMembers.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == familyMemberId.Value && f.UserId == userId))?.FirstName
                : null;
            throw new InvalidOperationException(RegistrationMessages.Describe(check, firstName));
        }

        var area = (await _areaService.GetAreaByIdAsync(areaId))!;

        if (familyMemberId.HasValue)
        {
            var familyMember = await _context.FamilyMembers
                .FirstOrDefaultAsync(f => f.Id == familyMemberId.Value && f.UserId == userId);
            if (familyMember == null)
                throw new InvalidOperationException("Selected family member not found.");
        }

        var appointment = new Appointment
        {
            UserId             = userId,
            AreaId             = areaId,
            FamilyMemberId     = familyMemberId,
            Status             = AppointmentStatus.Registered,
            Comment            = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            UseAlternativeSlot = useAlternativeSlot,
            // Nach 18:00 am Vorabend (oder am Tag selbst) keine Erinnerung mehr – die Bestätigung kam ja gerade
            ReminderSentAt     = ReminderSchedule.IsPastSendMoment(area.Date, DateTime.UtcNow) ? DateTime.UtcNow : null
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var sendQr = area.Event?.CheckInEnabled == true && IsVerkauf(area);
        await SendRegistrationMailsAsync(userId, familyMemberId, area, comment,
            sendQr ? appointment.Id : null, useAlternativeSlot);
        await _adminNotifications.NotifyRegistrationAsync(appointment.Id);
    }

    public async Task CancelAppointmentAsync(int appointmentId, string userId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(a => a.Event)
            .Include(a => a.Area)
                .ThenInclude(a => a.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.UserId == userId);

        if (appointment == null) return;

        appointment.Status = AppointmentStatus.Cancelled;
        await _context.SaveChangesAsync();

        // Fahrzeugzuweisung bereinigen falls Fahrer/Beifahrer-Bereich storniert wird
        await ClearVehicleAssignmentOnCancelAsync(userId, appointment);

        await SendCancellationMailAsync(userId, appointment);
        await _adminNotifications.NotifyRegistrationAsync(appointment.Id, cancelled: true);
    }

    private async Task ClearVehicleAssignmentOnCancelAsync(string userId, Appointment appointment)
    {
        var categoryName = appointment.Area?.AreaTemplate?.AreaCategory?.Name;
        if (categoryName != "Fahrer") return;

        var areaName = appointment.Area!.AreaTemplate!.Name; // "Fahrer" oder "Beifahrer"
        var date     = appointment.Area.Date.Date;
        var eventId  = appointment.Area.EventId;

        var assignmentDates = await _context.AssignmentDates
            .Include(d => d.Assignment)
            .Where(d => d.Assignment.EventId == eventId && d.Date.Date == date)
            .ToListAsync();

        bool changed = false;
        foreach (var ad in assignmentDates)
        {
            if (areaName == "Fahrer" && ad.DriverUserId == userId)
            {
                ad.DriverUserId = null;
                ad.DriverPhone  = null;
                changed = true;
            }
            else if (areaName == "Beifahrer" && ad.HelperUserId == userId)
            {
                ad.HelperUserId = null;
                changed = true;
            }
        }

        if (changed)
            await _context.SaveChangesAsync();
    }

    public async Task<bool> CanRegisterAsync(string userId, int areaId, int? familyMemberId = null, bool useAlternativeSlot = false)
        => (await CheckRegistrationAsync(userId, areaId, familyMemberId, useAlternativeSlot)).CanRegister;

    public async Task<RegistrationCheck> CheckRegistrationAsync(string userId, int areaId, int? familyMemberId = null, bool useAlternativeSlot = false)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null) return new RegistrationCheck(RegistrationBlock.AreaNotFound);

        // Zuerst: schon hier angemeldet (ganzer oder halber Tag) – das ist die verständlichste Antwort
        if (await PersonIsRegisteredForAreaAsync(userId, familyMemberId, areaId))
            return new RegistrationCheck(RegistrationBlock.AlreadyRegistered);

        if (useAlternativeSlot && !await PersonMayUseAlternativeSlotAsync(userId, familyMemberId, area))
            return new RegistrationCheck(RegistrationBlock.FullDayOnly);

        var minAge = area.AreaTemplate?.MinAge ?? 0;
        if (minAge > 0)
        {
            var age = await GetPersonAgeAsync(userId, familyMemberId);
            if (age.HasValue && age.Value < minAge)
                return new RegistrationCheck(RegistrationBlock.MinAge, MinAge: minAge, PersonAge: age);
        }

        var timeSlot = AreaSchedule.TimeSlot(area, useAlternativeSlot);
        var capacity = useAlternativeSlot && area.AlternativeMaxCapacity.HasValue
            ? area.AlternativeMaxCapacity.Value
            : area.MaxCapacity;

        var current = await _areaService.GetCurrentRegistrationsAsync(areaId, useAlternativeSlot);
        if (current >= capacity) return new RegistrationCheck(RegistrationBlock.Full);

        // Spontan: keine fixe Zeit, also auch keine Überschneidung
        if (area.IsSpontaneous) return RegistrationCheck.Ok;

        var conflict = await FindConflictingRegistrationAsync(userId, familyMemberId, area.Date, timeSlot);
        if (conflict != null)
            return new RegistrationCheck(RegistrationBlock.TimeConflict,
                OtherAreaName: conflict.Area.AreaTemplate?.Name,
                OtherDate:     conflict.Area.Date,
                OtherTimeSlot: AreaSchedule.TimeSlot(conflict.Area, conflict.UseAlternativeSlot));

        if (IsVerkauf(area))
        {
            var sale = await FindRegisteredSaleAreaAsync(userId, familyMemberId, area.EventId);
            if (sale != null)
                return new RegistrationCheck(RegistrationBlock.SaleLimit, OtherAreaName: sale.Area.AreaTemplate?.Name);
        }

        if (IsKuchen(area))
        {
            var cake = await FindRegisteredCategoryOnDateAsync(userId, familyMemberId, "Kuchen", area.Date.Date);
            if (cake != null)
                return new RegistrationCheck(RegistrationBlock.CakeLimit, OtherAreaName: cake.Area.AreaTemplate?.Name);
        }

        return RegistrationCheck.Ok;
    }

    // Halbtag: Stufen in AreaTemplate.MandatoryStufen (z.B. Pfadis, Pios, Leiter) können nur ganze Tage
    private async Task<bool> PersonMayUseAlternativeSlotAsync(string userId, int? familyMemberId, Area area)
    {
        if (string.IsNullOrEmpty(area.AreaTemplate?.MandatoryStufen)) return true;

        var stufe = familyMemberId.HasValue
            ? (await _context.FamilyMembers.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == familyMemberId.Value && f.UserId == userId))?.Stufe
            : (await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId))?.Stufe;

        return !AreaSchedule.IsFullDayOnly(area, stufe);
    }

    private async Task SendRegistrationMailsAsync(string userId, int? familyMemberId, Area area,
        string? comment = null, int? appointmentId = null, bool useAlternativeSlot = false)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.Email == null) return;

        var userName  = $"{user.FirstName} {user.LastName}";
        var eventName = area.Event?.Name ?? "";
        var areaName  = area.AreaTemplate?.Name ?? "";
        var forPerson = await GetPersonNameAsync(userId, familyMemberId);
        var timeSlot  = AreaSchedule.TimeSlot(area, useAlternativeSlot);
        var notifications = user.EmailNotificationsEnabled;

        // Capture all values; lambdas must not close over EF entities
        var email       = user.Email;
        var date        = AreaSchedule.Date(area);
        var aptId       = appointmentId;
        var person      = forPerson ?? userName.Trim(); // immer «Angemeldet für», auch beim Kontoinhaber

        if (notifications)
        {
            _mailQueue.Enqueue(m => m.SendRegistrationConfirmationAsync(
                email, userName, areaName, eventName, date, timeSlot, comment, aptId, person));
        }
    }

    private async Task SendCancellationMailAsync(string userId, Appointment appointment)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.Email == null) return;

        var userName  = $"{user.FirstName} {user.LastName}";
        var email     = user.Email;
        var areaName  = appointment.Area?.AreaTemplate?.Name ?? "";
        var eventName = appointment.Area?.Event?.Name ?? "";
        var date      = AreaSchedule.Date(appointment.Area);
        var timeSlot  = AreaSchedule.TimeSlot(appointment.Area, appointment.UseAlternativeSlot);
        var person    = await GetPersonNameAsync(userId, appointment.FamilyMemberId) ?? userName.Trim();
        var notifications = user.EmailNotificationsEnabled;

        if (notifications)
        {
            _mailQueue.Enqueue(m => m.SendCancellationConfirmationAsync(
                email, userName, areaName, eventName, date, timeSlot, person));
        }
    }

    private async Task<string?> GetPersonNameAsync(string userId, int? familyMemberId)
    {
        if (!familyMemberId.HasValue) return null;

        var member = await _context.FamilyMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == familyMemberId.Value && f.UserId == userId);

        return member != null ? $"{member.FirstName} {member.LastName}" : null;
    }

    private async Task<Appointment?> FindConflictingRegistrationAsync(
        string userId, int? familyMemberId, DateTime date, string timeSlot)
    {
        var existingAppointments = await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(ar => ar.AreaTemplate)
            .Where(a => a.UserId == userId && a.Status == AppointmentStatus.Registered)
            .Where(a => familyMemberId.HasValue
                ? a.FamilyMemberId == familyMemberId
                : a.FamilyMemberId == null)
            .Where(a => a.Area.Date.Date == date.Date && !a.Area.IsSpontaneous)
            .ToListAsync();

        // Mit der tatsächlich gebuchten Zeit vergleichen (bei Halbtag die halbe Schicht)
        return existingAppointments.FirstOrDefault(existing =>
            TimeSlotConflict(timeSlot, AreaSchedule.TimeSlot(existing.Area, existing.UseAlternativeSlot)));
    }

    private async Task<bool> PersonIsRegisteredForAreaAsync(string userId, int? familyMemberId, int areaId) =>
        await _context.Appointments
            .Where(a => a.UserId == userId && a.AreaId == areaId && a.Status == AppointmentStatus.Registered)
            .AnyAsync(a => familyMemberId.HasValue
                ? a.FamilyMemberId == familyMemberId
                : a.FamilyMemberId == null);

    private async Task<int?> GetPersonAgeAsync(string userId, int? familyMemberId)
    {
        if (familyMemberId.HasValue)
        {
            var familyMember = await _context.FamilyMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == familyMemberId.Value && f.UserId == userId);

            return familyMember != null
                ? CalculateAge(familyMember.Birthday, DateTime.UtcNow.Date)
                : null;
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user?.Birthday.HasValue == true
            ? CalculateAge(user.Birthday!.Value, DateTime.UtcNow.Date)
            : null;
    }

    private int CalculateAge(DateTime birthDate, DateTime referenceDate)
    {
        var age = referenceDate.Year - birthDate.Year;
        if (referenceDate < birthDate.AddYears(age)) age--;
        return age;
    }

    private async Task<Appointment?> FindRegisteredCategoryOnDateAsync(
        string userId, int? familyMemberId, string categoryName, DateTime date)
    {
        return await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(ar => ar.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .Where(a => a.UserId == userId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.Date.Date == date
                     && a.Area.AreaTemplate != null
                     && a.Area.AreaTemplate.AreaCategory != null
                     && a.Area.AreaTemplate.AreaCategory.Name == categoryName)
            .Where(a => familyMemberId.HasValue
                ? a.FamilyMemberId == familyMemberId
                : a.FamilyMemberId == null)
            .FirstOrDefaultAsync();
    }

    private async Task<Appointment?> FindRegisteredSaleAreaAsync(
        string userId, int? familyMemberId, int eventId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(area => area.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .Where(a => a.UserId == userId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.EventId == eventId)
            .Where(a => familyMemberId.HasValue
                ? a.FamilyMemberId == familyMemberId
                : a.FamilyMemberId == null)
            .FirstOrDefaultAsync(a => a.Area.AreaTemplate != null
                        && a.Area.AreaTemplate.AreaCategory != null
                        && a.Area.AreaTemplate.AreaCategory.Name == "Verkauf");
    }

    private bool TimeSlotConflict(string slot1, string slot2)
    {
        var times1 = ExtractTimeRange(slot1);
        var times2 = ExtractTimeRange(slot2);

        if (times1 == null || times2 == null) return false;

        var (start1, end1) = times1.Value;
        var (start2, end2) = times2.Value;

        return start1 < end2 && start2 < end1;
    }

    private (int, int)? ExtractTimeRange(string timeSlot)
    {
        if (string.IsNullOrWhiteSpace(timeSlot)) return null;

        try
        {
            var parts = timeSlot.Split('-');
            if (parts.Length != 2) return null;

            var start = int.Parse(parts[0].Replace(":", ""));
            var end   = int.Parse(parts[1].Replace(":", ""));

            return (start, end);
        }
        catch { return null; }
    }

    public async Task<Appointment?> GetByIdForCheckInAsync(int appointmentId)
    {
        return await _context.Appointments
            .Include(a => a.User)
            .Include(a => a.FamilyMember)
            .Include(a => a.Area).ThenInclude(ar => ar.AreaTemplate).ThenInclude(t => t!.AreaCategory)
            .Include(a => a.Area).ThenInclude(ar => ar.Event)
            .FirstOrDefaultAsync(a => a.Id == appointmentId
                                   && a.Status == AppointmentStatus.Registered);
    }

    public async Task<bool> CheckInAsync(int appointmentId)
    {
        var apt = await _context.Appointments.FindAsync(appointmentId);
        if (apt == null || apt.Status != AppointmentStatus.Registered) return false;
        apt.CheckedInAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CheckOutAsync(int appointmentId)
    {
        var apt = await _context.Appointments.FindAsync(appointmentId);
        if (apt == null || !apt.CheckedInAt.HasValue || apt.CheckedOutAt.HasValue) return false;
        apt.CheckedOutAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }
}
