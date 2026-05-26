using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAreaService _areaService;

    public AppointmentService(ApplicationDbContext context, IAreaService areaService)
    {
        _context = context;
        _areaService = areaService;
    }

    public async Task<List<Appointment>> GetUserAppointmentsAsync(string userId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(area => area.Event)
            .Include(a => a.Area)
                .ThenInclude(area => area.AreaCategory)   // ← neu
            .Include(a => a.User)
            .Include(a => a.FamilyMember)
            .Where(a => a.UserId == userId)
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

        existingMember.FirstName  = familyMember.FirstName;
        existingMember.LastName   = familyMember.LastName;
        existingMember.Pfadiname  = familyMember.Pfadiname;
        existingMember.Stufe      = familyMember.Stufe;
        existingMember.Birthday   = familyMember.Birthday;

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

    // ── Hilfsmethode: prüft ob eine Area die Kategorie "Verkauf" hat ──────────
    private bool IsVerkauf(Area area)
        => area.AreaCategory?.Name == "Verkauf";

    public async Task RegisterForAppointmentAsync(string userId, int areaId, int? familyMemberId = null)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null)
            throw new InvalidOperationException("Area not found.");

        if (area.MinAge > 0 && await IsPersonBelowMinAgeAsync(userId, familyMemberId, area.MinAge))
            throw new InvalidOperationException($"Person erfüllt das Mindestalter von {area.MinAge} Jahren nicht.");

        if (!await CanRegisterAsync(userId, areaId, familyMemberId))
        {
            var message = IsVerkauf(area)
                ? "Du kannst dich nur für einen Verkauf-Bereich registrieren."
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
            UserId         = userId,
            AreaId         = areaId,
            FamilyMemberId = familyMemberId,
            Status         = AppointmentStatus.Registered
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CanRegisterAsync(string userId, int areaId, int? familyMemberId = null)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null)
            return false;

        if (area.MinAge > 0 && await IsPersonBelowMinAgeAsync(userId, familyMemberId, area.MinAge))
            return false;

        var current = await _areaService.GetCurrentRegistrationsAsync(areaId);
        if (current >= area.MaxCapacity)
            return false;

        if (await PersonHasConflictingRegistrationAsync(userId, familyMemberId, area.Date, area.TimeSlot))
            return false;

        // Verkauf-Regel: nur eine Verkauf-Area pro Event erlaubt
        if (IsVerkauf(area))
            return !await PersonHasRegisteredSaleAreaAsync(userId, familyMemberId, area.EventId);

        return true;
    }

    // Overload ohne familyMemberId (für Rückwärtskompatibilität)
    public async Task<bool> CanRegisterAsync(string userId, int areaId)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null)
            return false;

        var current = await _areaService.GetCurrentRegistrationsAsync(areaId);
        if (current >= area.MaxCapacity)
            return false;

        if (await UserHasConflictingRegistrationAsync(userId, area.Date, area.TimeSlot))
            return false;

        if (IsVerkauf(area))
            return !await UserHasRegisteredSaleAreaAsync(userId, area.EventId);

        return true;
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

    private async Task<bool> PersonHasRegisteredSaleAreaAsync(string userId, int? familyMemberId, int eventId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
                .ThenInclude(area => area.AreaCategory)   // ← neu
            .Where(a => a.UserId == userId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.EventId == eventId)
            .Where(a => familyMemberId.HasValue
                ? a.FamilyMemberId == familyMemberId
                : a.FamilyMemberId == null)
            .AnyAsync(a => a.Area.AreaCategory != null && a.Area.AreaCategory.Name == "Verkauf");
    }

    public async Task CancelAppointmentAsync(int appointmentId, string userId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.UserId == userId);

        if (appointment != null)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<bool> UserHasConflictingRegistrationAsync(string userId, DateTime date, string timeSlot)
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
                .ThenInclude(area => area.AreaCategory)   // ← neu
            .Where(a => a.UserId == userId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.EventId == eventId)
            .AnyAsync(a => a.Area.AreaCategory != null && a.Area.AreaCategory.Name == "Verkauf");
    }
}