using FlomiApp.Data.Models;
using FlomiApp.Services;
using Xunit;

namespace FlomiApp.Tests.Services;

public class VehicleServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    private VehicleService CreateService()
    {
        using var setupCtx = _db.CreateContext();
        var userManager = TestUserManager.Create(setupCtx);
        return new VehicleService(_db.CreateContextFactory(), userManager);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAlreadyAssignedUserIdsByDateAsync_ReturnsDriverAndHelper_ForEachDate()
    {
        using var ctx = _db.CreateContext();
        var evt = new Event { Name = "Flomi", Date = new DateTime(2026, 11, 20) };
        ctx.Events.Add(evt);
        var vehicle = new Vehicle { OwnerName = "Halter" };
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();

        var assignment = new VehicleAssignment { VehicleId = vehicle.Id, EventId = evt.Id };
        ctx.VehicleAssignments.Add(assignment);
        await ctx.SaveChangesAsync();

        var day1 = new DateTime(2026, 11, 19);
        var day2 = new DateTime(2026, 11, 20);
        ctx.AssignmentDates.AddRange(
            new AssignmentDate { AssignmentId = assignment.Id, Date = day1, DriverUserId = "driver1", HelperUserId = "helper1" },
            new AssignmentDate { AssignmentId = assignment.Id, Date = day2, DriverUserId = "driver2" }
        );
        await ctx.SaveChangesAsync();

        var result = await CreateService().GetAlreadyAssignedUserIdsByDateAsync(evt.Id, Array.Empty<int>());

        Assert.Equal(new HashSet<string> { "driver1", "helper1" }, result[day1]);
        Assert.Equal(new HashSet<string> { "driver2" }, result[day2]);
    }

    [Fact]
    public async Task GetAlreadyAssignedUserIdsByDateAsync_ExcludesSpecifiedAssignment()
    {
        // Simuliert das Bearbeiten einer bestehenden Zuweisung: deren eigene
        // Belegung darf sich nicht selbst als "bereits vergeben" blockieren.
        using var ctx = _db.CreateContext();
        var evt = new Event { Name = "Flomi", Date = new DateTime(2026, 11, 20) };
        ctx.Events.Add(evt);
        var vehicle = new Vehicle { OwnerName = "Halter" };
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();

        var assignmentToEdit = new VehicleAssignment { VehicleId = vehicle.Id, EventId = evt.Id };
        var otherAssignment  = new VehicleAssignment { VehicleId = vehicle.Id, EventId = evt.Id };
        ctx.VehicleAssignments.AddRange(assignmentToEdit, otherAssignment);
        await ctx.SaveChangesAsync();

        var date = new DateTime(2026, 11, 20);
        ctx.AssignmentDates.AddRange(
            new AssignmentDate { AssignmentId = assignmentToEdit.Id, Date = date, DriverUserId = "self-driver" },
            new AssignmentDate { AssignmentId = otherAssignment.Id, Date = date, DriverUserId = "other-driver" }
        );
        await ctx.SaveChangesAsync();

        var result = await CreateService().GetAlreadyAssignedUserIdsByDateAsync(evt.Id, new[] { assignmentToEdit.Id });

        Assert.Equal(new HashSet<string> { "other-driver" }, result[date]);
    }

    [Fact]
    public async Task CreateAssignmentAsync_DeduplicatesDatesByDay()
    {
        using var ctx = _db.CreateContext();
        var evt = new Event { Name = "Flomi", Date = new DateTime(2026, 11, 20) };
        ctx.Events.Add(evt);
        var vehicle = new Vehicle { OwnerName = "Halter" };
        ctx.Vehicles.Add(vehicle);
        await ctx.SaveChangesAsync();

        var assignment = new VehicleAssignment { VehicleId = vehicle.Id, EventId = evt.Id };
        var sameDayTwice = new List<AssignmentDate>
        {
            new() { Date = new DateTime(2026, 11, 20, 8, 0, 0), DriverUserId = "first" },
            new() { Date = new DateTime(2026, 11, 20, 18, 0, 0), DriverUserId = "second" }
        };

        await CreateService().CreateAssignmentAsync(assignment, sameDayTwice);

        using var verifyCtx = _db.CreateContext();
        var saved = verifyCtx.AssignmentDates.Where(d => d.AssignmentId == assignment.Id).ToList();
        Assert.Single(saved);
    }
}
