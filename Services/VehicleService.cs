using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class VehicleService : IVehicleService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly UserManager<ApplicationUser>            _userManager;

    public VehicleService(IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<ApplicationUser> userManager)
    {
        _dbFactory   = dbFactory;
        _userManager = userManager;
    }

    // ── Stammdaten ────────────────────────────────────────────────────────────
    public async Task<List<Vehicle>> GetAllVehiclesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Vehicles
            .AsNoTracking()
            .Include(v => v.Assignments)
            .OrderBy(v => v.OwnerName)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task CreateVehicleAsync(Vehicle vehicle)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
    }

    public async Task UpdateVehicleAsync(Vehicle vehicle)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        if (existing is null) return;

        existing.OwnerName    = vehicle.OwnerName;
        existing.OwnerPhone   = vehicle.OwnerPhone;
        existing.OwnerContact = vehicle.OwnerContact;

        await db.SaveChangesAsync();
    }

    public async Task DeleteVehicleAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var vehicle = await db.Vehicles
            .Include(v => v.Assignments)
                .ThenInclude(a => a.AssignedDates)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehicle is null) return;

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync();
    }

    // ── Zuweisungen ───────────────────────────────────────────────────────────
    public async Task<List<VehicleAssignment>> GetAssignmentsByEventAsync(int eventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.VehicleAssignments
            .Include(a => a.Vehicle)
            .Include(a => a.AssignedDates)
            .AsNoTracking()
            .AsQueryable();

        if (eventId > 0)
            query = query.Where(a => a.EventId == eventId);

        return await query
            .OrderBy(a => a.Vehicle.OwnerName)
            .ToListAsync();
    }

    public async Task CreateAssignmentAsync(VehicleAssignment assignment, List<AssignmentDate> dates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        assignment.AssignedDates = dates
            .GroupBy(d => d.Date.Date)
            .Select(g => g.First())
            .Select(CopyDay)
            .ToList();

        db.VehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAssignmentAsync(VehicleAssignment assignment, List<AssignmentDate> dates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.VehicleAssignments
            .Include(a => a.AssignedDates)
            .FirstOrDefaultAsync(a => a.Id == assignment.Id);

        if (existing is null) return;

        db.AssignmentDates.RemoveRange(existing.AssignedDates);

        existing.AssignedDates = dates
            .GroupBy(d => d.Date.Date)
            .Select(g => g.First())
            .Select(CopyDay)
            .ToList();

        await db.SaveChangesAsync();
    }

    public async Task DeleteAssignmentAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var assignment = await db.VehicleAssignments
            .Include(a => a.AssignedDates)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment is null) return;

        db.VehicleAssignments.Remove(assignment);
        await db.SaveChangesAsync();
    }

    private static AssignmentDate CopyDay(AssignmentDate d) => new()
    {
        Date         = d.Date.Date,
        DriverUserId = d.DriverUserId,
        DriverName   = d.DriverName,
        DriverPhone  = d.DriverPhone,
        HelperUserId = d.HelperUserId,
        HelperName   = d.HelperName,
        ReadyFrom    = d.ReadyFrom,
        PickedUpBy   = d.PickedUpBy,
        ReturnedBy   = d.ReturnedBy,
    };

    // ── Einsatzplan ───────────────────────────────────────────────────────────
    public async Task SaveAssignmentDayAsync(int vehicleId, int eventId, AssignmentDate day)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var date     = day.Date.Date;
        var existing = await db.VehicleAssignments
            .Include(a => a.AssignedDates)
            .Where(a => a.VehicleId == vehicleId && a.EventId == eventId)
            .OrderBy(a => a.Id)
            .ToListAsync();

        // Ältere Daten können pro Fahrzeug und Event mehrere Zuweisungen haben – den Tag überall ersetzen
        foreach (var a in existing)
            db.AssignmentDates.RemoveRange(a.AssignedDates.Where(d => d.Date.Date == date));

        var target = existing.FirstOrDefault();
        if (target is null)
        {
            target = new VehicleAssignment { VehicleId = vehicleId, EventId = eventId };
            db.VehicleAssignments.Add(target);
        }

        target.AssignedDates.Add(CopyDay(day));
        await db.SaveChangesAsync();
    }

    public async Task RemoveAssignmentDayAsync(int vehicleId, int eventId, DateTime date)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.VehicleAssignments
            .Include(a => a.AssignedDates)
            .Where(a => a.VehicleId == vehicleId && a.EventId == eventId)
            .ToListAsync();

        foreach (var a in existing)
        {
            var remove = a.AssignedDates.Where(d => d.Date.Date == date.Date).ToList();
            db.AssignmentDates.RemoveRange(remove);
            // Ohne Einsatztage bleibt keine leere Zuweisung stehen
            if (a.AssignedDates.Count == remove.Count)
                db.VehicleAssignments.Remove(a);
        }

        await db.SaveChangesAsync();
    }

    public async Task<CrewAssignmentResult> AutoAssignCrewAsync(int eventId, DateTime? date = null)
    {
        var drivers = await GetUsersByAreaNameAndEventAsync(eventId, VehicleCrewAreas.Driver);
        var helpers = await GetUsersByAreaNameAndEventAsync(eventId, VehicleCrewAreas.Helper);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var days = await db.AssignmentDates
            .Include(d => d.Assignment)
                .ThenInclude(a => a.Vehicle)
            .Where(d => d.Assignment.EventId == eventId)
            .ToListAsync();

        int driversAssigned = 0, helpersAssigned = 0;

        foreach (var dayGroup in days.GroupBy(d => d.Date.Date))
        {
            if (date.HasValue && dayGroup.Key != date.Value.Date) continue;

            var slots = dayGroup.OrderBy(d => d.Assignment.Vehicle.OwnerName).ToList();
            // Wer an diesem Tag schon auf einem Fahrzeug ist, wird nicht nochmals eingeteilt
            var busy  = slots.SelectMany(d => new[] { d.DriverUserId, d.HelperUserId })
                             .Where(id => id != null).Select(id => id!)
                             .ToHashSet();

            var freeDrivers = new Queue<ApplicationUser>(
                drivers.GetValueOrDefault(dayGroup.Key, new()).Where(u => !busy.Contains(u.Id)));
            foreach (var slot in slots.Where(s => !s.HasDriver))
            {
                if (!freeDrivers.TryDequeue(out var u)) break;
                slot.DriverUserId = u.Id;
                slot.DriverPhone  = PhoneHelper.FormatSwissPhone(u.PhoneNumber) ?? slot.DriverPhone;
                busy.Add(u.Id);
                driversAssigned++;
            }

            // Übrig gebliebene Fahrer springen als Helfer ein, wenn die Beifahrer nicht reichen
            var freeHelpers = new Queue<ApplicationUser>(
                helpers.GetValueOrDefault(dayGroup.Key, new())
                       .Concat(freeDrivers)
                       .Where(u => !busy.Contains(u.Id))
                       .DistinctBy(u => u.Id));
            foreach (var slot in slots.Where(s => !s.HasHelper))
            {
                if (!freeHelpers.TryDequeue(out var u)) break;
                slot.HelperUserId = u.Id;
                busy.Add(u.Id);
                helpersAssigned++;
            }
        }

        await db.SaveChangesAsync();
        return new CrewAssignmentResult(driversAssigned, helpersAssigned);
    }

    public async Task<int> CopyAssignmentsFromEventAsync(int sourceEventId, int targetEventId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var source = await db.Events.FindAsync(sourceEventId);
        var target = await db.Events.FindAsync(targetEventId);
        if (source is null || target is null || sourceEventId == targetEventId) return 0;

        // Tage relativ zum Flomi übernehmen: «2 Tage vorher» bleibt «2 Tage vorher»
        var offset = target.Date.Date - source.Date.Date;

        var alreadyInTarget = await db.VehicleAssignments
            .Where(a => a.EventId == targetEventId)
            .Select(a => a.VehicleId)
            .ToListAsync();

        var sourceAssignments = await db.VehicleAssignments
            .Include(a => a.AssignedDates)
            .Where(a => a.EventId == sourceEventId
                     && !a.Vehicle.IsSample
                     && !alreadyInTarget.Contains(a.VehicleId))
            .AsNoTracking()
            .ToListAsync();

        var copied = 0;
        foreach (var group in sourceAssignments.GroupBy(a => a.VehicleId))
        {
            // Fahrer und Helfer wechseln jedes Jahr – übernommen werden nur Tag und «Bereit ab»
            var dates = group
                .SelectMany(a => a.AssignedDates)
                .GroupBy(d => d.Date.Date)
                .Select(g => new AssignmentDate { Date = g.Key + offset, ReadyFrom = g.First().ReadyFrom })
                .ToList();
            if (dates.Count == 0) continue;

            db.VehicleAssignments.Add(new VehicleAssignment
            {
                VehicleId     = group.Key,
                EventId       = targetEventId,
                AssignedDates = dates,
            });
            copied++;
        }

        await db.SaveChangesAsync();
        return copied;
    }

    public async Task<List<VehicleDayCoverage>> GetDayCoverageAsync(IEnumerable<int> eventIds)
    {
        var ids = eventIds.ToList();
        await using var db = await _dbFactory.CreateDbContextAsync();

        var days = await db.AssignmentDates
            .Where(d => ids.Contains(d.Assignment.EventId))
            .Select(d => new
            {
                d.Assignment.EventId,
                d.Assignment.VehicleId,
                d.Date,
                HasDriver = d.DriverUserId != null || (d.DriverName != null && d.DriverName != ""),
            })
            .ToListAsync();

        var pickups = await db.FurniturePickupRequests
            .Where(r => r.EventId != null && ids.Contains(r.EventId.Value)
                     && r.Status == PickupRequestStatus.Accepted)
            .Select(r => new { EventId = r.EventId!.Value, r.PickupDate })
            .ToListAsync();

        var keys = days.Select(d => (d.EventId, Date: d.Date.Date))
            .Concat(pickups.Select(p => (p.EventId, Date: p.PickupDate.Date)))
            .Distinct();

        return keys
            .Select(k =>
            {
                var vehiclesOnDay = days.Where(d => d.EventId == k.EventId && d.Date.Date == k.Date)
                                        .GroupBy(d => d.VehicleId)
                                        .ToList();
                return new VehicleDayCoverage(
                    k.EventId,
                    k.Date,
                    pickups.Count(p => p.EventId == k.EventId && p.PickupDate.Date == k.Date),
                    vehiclesOnDay.Count,
                    vehiclesOnDay.Count(g => g.Any(d => d.HasDriver)));
            })
            .OrderBy(c => c.Date)
            .ToList();
    }

    // ── Users ─────────────────────────────────────────────────────────────────
    public async Task<Dictionary<DateTime, HashSet<string>>> GetAlreadyAssignedUserIdsByDateAsync(int eventId, IEnumerable<int> excludeAssignmentIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var exclude = excludeAssignmentIds.ToHashSet();

        var dates = await db.AssignmentDates
            .Include(d => d.Assignment)
            .Where(d => d.Assignment.EventId == eventId && !exclude.Contains(d.AssignmentId))
            .AsNoTracking()
            .ToListAsync();

        return dates
            .GroupBy(d => d.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(d => new[] { d.DriverUserId, d.HelperUserId }
                         .Where(id => id != null).Select(id => id!))
                     .ToHashSet()
            );
    }

    public async Task<List<AssignmentDate>> GetVehicleAssignmentsForUserAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AssignmentDates
            .Include(d => d.Assignment)
                .ThenInclude(a => a.Vehicle)
            .Include(d => d.Assignment)
                .ThenInclude(a => a.Event)
            .Where(d => d.DriverUserId == userId || d.HelperUserId == userId)
            .OrderBy(d => d.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    public async Task<Dictionary<DateTime, List<ApplicationUser>>> GetUsersByAreaNameAndEventAsync(int eventId, string areaTemplateName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entries = await db.Appointments
            .Include(a => a.Area)
                .ThenInclude(ar => ar.AreaTemplate)
            .Where(a => a.Area.EventId == eventId
                     && a.Status == AppointmentStatus.Registered
                     && a.Area.AreaTemplate != null
                     && a.Area.AreaTemplate.Name == areaTemplateName)
            .Select(a => new { a.UserId, Date = a.Area.Date.Date })
            .Distinct()
            .ToListAsync();

        var userIds = entries.Select(e => e.UserId).Distinct().ToList();
        var users   = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        var userDict = users.ToDictionary(u => u.Id);

        return entries
            .GroupBy(e => e.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.UserId)
                       .Distinct()
                       .Where(id => userDict.ContainsKey(id))
                       .Select(id => userDict[id])
                       .ToList()
            );
    }
}
