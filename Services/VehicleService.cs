using FlomiApp.Data;
using FlomiApp.Data.Models;
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
            .Select(d => new AssignmentDate
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
            })
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
            .Select(d => new AssignmentDate
            {
                AssignmentId = existing.Id,
                Date         = d.Date.Date,
                DriverUserId = d.DriverUserId,
                DriverName   = d.DriverName,
                DriverPhone  = d.DriverPhone,
                HelperUserId = d.HelperUserId,
                HelperName   = d.HelperName,
                ReadyFrom    = d.ReadyFrom,
                PickedUpBy   = d.PickedUpBy,
                ReturnedBy   = d.ReturnedBy,
            })
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
