using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAreaService _areaService;
    private readonly IMailService _mailService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        ApplicationDbContext context,
        IAreaService areaService,
        IMailService mailService,
        ILogger<AppointmentService> logger)
    {
        _context     = context;
        _areaService = areaService;
        _mailService = mailService;
        _logger      = logger;
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
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null)
            throw new InvalidOperationException("Area not found.");

        var minAge = area.AreaTemplate?.MinAge ?? 0;
        if (minAge > 0 && await IsPersonBelowMinAgeAsync(userId, familyMemberId, minAge))
            throw new InvalidOperationException(
                $"Person erfüllt das Mindestalter von {minAge} Jahren nicht.");

        if (!await CanRegisterAsync(userId, areaId, familyMemberId))
        {
            var message = IsVerkauf(area) ? "Du kannst dich nur für einen Verkauf-Bereich registrieren."
                        : IsKuchen(area)  ? "Du kannst dich pro Tag nur einmal für Kuchen anmelden."
                                          : "Der Bereich ist voll.";
            throw new InvalidOperationException(message);
        }

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
            UseAlternativeSlot = useAlternativeSlot
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var sendQr = area.Event?.CheckInEnabled == true && IsVerkauf(area);
        await SendRegistrationMailsAsync(userId, familyMemberId, area, comment,
            sendQr ? appointment.Id : null, useAlternativeSlot);
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
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null) return false;

        var minAge = area.AreaTemplate?.MinAge ?? 0;
        if (minAge > 0 && await IsPersonBelowMinAgeAsync(userId, familyMemberId, minAge))
            return false;

        var timeSlot = useAlternativeSlot && !string.IsNullOrEmpty(area.AlternativeTimeSlot)
            ? area.AlternativeTimeSlot
            : area.TimeSlot;
        var capacity = useAlternativeSlot && area.AlternativeMaxCapacity.HasValue
            ? area.AlternativeMaxCapacity.Value
            : area.MaxCapacity;

        var current = await _areaService.GetCurrentRegistrationsAsync(areaId, useAlternativeSlot);
        if (current >= capacity) return false;

        if (await PersonHasConflictingRegistrationAsync(userId, familyMemberId, area.Date, timeSlot))
            return false;

        if (IsVerkauf(area))
            return !await PersonHasRegisteredSaleAreaAsync(userId, familyMemberId, area.EventId);

        if (IsKuchen(area))
            return !await PersonHasRegisteredCategoryOnDateAsync(userId, familyMemberId, "Kuchen", area.Date.Date);

        return true;
    }

    public async Task<bool> CanRegisterAsync(string userId, int areaId)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null) return false;

        var current = await _areaService.GetCurrentRegistrationsAsync(areaId, false);
        if (current >= area.MaxCapacity) return false;

        if (await UserHasConflictingRegistrationAsync(userId, area.Date, area.TimeSlot))
            return false;

        if (IsVerkauf(area))
            return !await UserHasRegisteredSaleAreaAsync(userId, area.EventId);

        if (IsKuchen(area))
            return !await PersonHasRegisteredCategoryOnDateAsync(userId, null, "Kuchen", area.Date.Date);

        return true;
    }

    private async Task SendRegistrationMailsAsync(string userId, int? familyMemberId, Area area,
        string? comment = null, int? appointmentId = null, bool useAlternativeSlot = false)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Email == null) return;

            var userName   = $"{user.FirstName} {user.LastName}";
            var eventName  = area.Event?.Name ?? "";
            var areaName   = area.AreaTemplate?.Name ?? "";
            var forPerson  = await GetPersonNameAsync(userId, familyMemberId);
            var timeSlot   = useAlternativeSlot && !string.IsNullOrEmpty(area.AlternativeTimeSlot)
                ? area.AlternativeTimeSlot
                : area.TimeSlot;

            if (user.EmailNotificationsEnabled)
            {
                await _mailService.SendRegistrationConfirmationAsync(
                    user.Email,
                    userName,
                    areaName,
                    eventName,
                    area.Date,
                    timeSlot,
                    comment,
                    appointmentId,
                    forPerson);
            }

            await _mailService.SendAdminNewRegistrationAsync(
                areaName,
                eventName,
                forPerson ?? userName,
                area.Date,
                timeSlot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MAIL FEHLER DETAIL: {Message}", ex.Message);
        }
    }

    private async Task SendCancellationMailAsync(string userId, Appointment appointment)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Email == null) return;

            var userName  = $"{user.FirstName} {user.LastName}";
            var areaName  = appointment.Area?.AreaTemplate?.Name ?? "";
            var eventName = appointment.Area?.Event?.Name ?? "";
            var date      = appointment.Area?.Date ?? DateTime.MinValue;
            var timeSlot  = appointment.Area?.TimeSlot ?? "";

            if (user.EmailNotificationsEnabled)
            {
                await _mailService.SendCancellationConfirmationAsync(
                    user.Email,
                    userName,
                    areaName,
                    eventName,
                    date,
                    timeSlot);
            }

            await _mailService.SendAdminCancellationAsync(
                areaName,
                eventName,
                userName,
                date,
                timeSlot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Senden der Storno-Mail für UserId {UserId}", userId);
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

    private async Task<bool> PersonHasConflictingRegistrationAsync(
        string userId, int? familyMemberId, DateTime date, string timeSlot)
    {
        var existingAppointments = await _context.Appointments
            .Include(a => a.Area)
            .Where(a => a.UserId == userId && a.Status == AppointmentStatus.Registered)
            .Where(a => familyMemberId.HasValue
                ? a.FamilyMemberId == familyMemberId
                : a.FamilyMemberId == null)
            .Where(a => a.Area.Date.Date == date.Date)
            .ToListAsync();

        foreach (var existing in existingAppointments)
        {
            if (TimeSlotConflict(timeSlot, existing.Area.TimeSlot))
                return true;
        }

        return false;
    }

    private async Task<bool> IsPersonBelowMinAgeAsync(string userId, int? familyMemberId, int minAge)
    {
        int? age = await GetPersonAgeAsync(userId, familyMemberId);
        return age.HasValue && age.Value < minAge;
    }

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

    private async Task<bool> PersonHasRegisteredCategoryOnDateAsync(
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
            .AnyAsync();
    }

    private async Task<bool> PersonHasRegisteredSaleAreaAsync(
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
            .AnyAsync(a => a.Area.AreaTemplate != null
                        && a.Area.AreaTemplate.AreaCategory != null
                        && a.Area.AreaTemplate.AreaCategory.Name == "Verkauf");
    }

    private async Task<bool> UserHasConflictingRegistrationAsync(
        string userId, DateTime date, string timeSlot)
    {
        var existingAppointments = await _context.Appointments
            .Include(a => a.Area)
            .Where(a => a.UserId == userId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.Date.Date == date.Date)
            .ToListAsync();

        foreach (var existing in existingAppointments)
        {
            if (TimeSlotConflict(timeSlot, existing.Area.TimeSlot))
                return true;
        }

        return false;
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

    private async Task<bool> UserHasRegisteredSaleAreaAsync(string userId, int eventId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(area => area.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .Where(a => a.UserId == userId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.EventId == eventId)
            .AnyAsync(a => a.Area.AreaTemplate != null
                        && a.Area.AreaTemplate.AreaCategory != null
                        && a.Area.AreaTemplate.AreaCategory.Name == "Verkauf");
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
