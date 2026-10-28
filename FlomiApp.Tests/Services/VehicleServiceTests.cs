using FlomiApp.Data.Models;
using FlomiApp.Services;
using Xunit;

namespace FlomiApp.Tests.Services;

public class VehicleServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    private readonly List<IDisposable> _userContexts = new();

    private VehicleService CreateService()
    {
        // Der UserManager liest über diesen Context – er muss bis Testende offen bleiben
        var userCtx = _db.CreateContext();
        _userContexts.Add(userCtx);
        return new VehicleService(_db.CreateContextFactory(), TestUserManager.Create(userCtx));
    }

    public void Dispose()
    {
        _userContexts.ForEach(c => c.Dispose());
        _db.Dispose();
    }

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

    // ── Einsatzplan ──────────────────────────────────────────────────────────

    private static readonly DateTime FlomiDay = new(2026, 11, 20);

    private async Task<(Event evt, Vehicle a, Vehicle b)> SeedPlanAsync()
    {
        using var ctx = _db.CreateContext();
        var evt = new Event { Name = "Flomi", Date = FlomiDay };
        var a   = new Vehicle { OwnerName = "Arnold" };
        var b   = new Vehicle { OwnerName = "Berger" };
        ctx.Events.Add(evt);
        ctx.Vehicles.AddRange(a, b);
        await ctx.SaveChangesAsync();
        return (evt, a, b);
    }

    private async Task RegisterForAreaAsync(Event evt, string areaName, DateTime date, params string[] userIds)
    {
        using var ctx = _db.CreateContext();
        var category = ctx.AreaCategories.FirstOrDefault() ?? new AreaCategory { Name = "Transport", SortOrder = 1 };
        var template = new AreaTemplate { Name = areaName, AreaCategory = category };
        var area     = new Area { EventId = evt.Id, AreaTemplate = template, Date = date, TimeSlot = "08:00-17:00", MaxCapacity = 10 };
        ctx.Areas.Add(area);

        foreach (var id in userIds)
        {
            if (ctx.Users.Find(id) == null)
                ctx.Users.Add(new ApplicationUser
                {
                    Id = id, UserName = id, Email = $"{id}@example.com",
                    FirstName = id, LastName = id, PhoneNumber = "0791234567",
                });
            ctx.Appointments.Add(new Appointment { UserId = id, Area = area });
        }
        await ctx.SaveChangesAsync();
    }

    private List<AssignmentDate> DaysOf(int eventId)
    {
        using var ctx = _db.CreateContext();
        return ctx.AssignmentDates
            .Where(d => d.Assignment.EventId == eventId)
            .Select(d => new { d, d.Assignment.VehicleId })
            .AsEnumerable()
            .Select(x => { x.d.Assignment = new VehicleAssignment { VehicleId = x.VehicleId }; return x.d; })
            .ToList();
    }

    [Fact]
    public async Task SaveAssignmentDayAsync_CreatesAssignment_ThenReplacesSameDay()
    {
        var (evt, a, _) = await SeedPlanAsync();
        var service = CreateService();

        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay, DriverName = "Anna" });
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay, DriverName = "Beat" });
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay.AddDays(-1) });

        using var ctx = _db.CreateContext();
        Assert.Single(ctx.VehicleAssignments.Where(x => x.EventId == evt.Id));
        var days = DaysOf(evt.Id);
        Assert.Equal(2, days.Count);
        Assert.Equal("Beat", days.Single(d => d.Date == FlomiDay).DriverName);
    }

    [Fact]
    public async Task RemoveAssignmentDayAsync_DeletesAssignment_WhenLastDayRemoved()
    {
        var (evt, a, _) = await SeedPlanAsync();
        var service = CreateService();
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay });
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay.AddDays(-1) });

        await service.RemoveAssignmentDayAsync(a.Id, evt.Id, FlomiDay);
        using (var ctx = _db.CreateContext())
            Assert.Single(ctx.VehicleAssignments.Where(x => x.EventId == evt.Id));

        await service.RemoveAssignmentDayAsync(a.Id, evt.Id, FlomiDay.AddDays(-1));
        using (var ctx = _db.CreateContext())
            Assert.Empty(ctx.VehicleAssignments.Where(x => x.EventId == evt.Id));
    }

    [Fact]
    public async Task AutoAssignCrewAsync_FillsEmptySlots_WithRegisteredUsers()
    {
        var (evt, a, b) = await SeedPlanAsync();
        await RegisterForAreaAsync(evt, "Fahrer", FlomiDay, "anna", "beat");
        await RegisterForAreaAsync(evt, "Beifahrer", FlomiDay, "carla");
        var service = CreateService();
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay });
        await service.SaveAssignmentDayAsync(b.Id, evt.Id, new AssignmentDate { Date = FlomiDay });

        var result = await service.AutoAssignCrewAsync(evt.Id);

        Assert.Equal(new CrewAssignmentResult(2, 1), result);
        var days = DaysOf(evt.Id);
        Assert.Equal(new[] { "anna", "beat" }, days.Select(d => d.DriverUserId).OrderBy(x => x));
        Assert.Contains(days, d => d.HelperUserId == "carla");
        Assert.All(days, d => Assert.Equal("+41 79 123 45 67", d.DriverPhone));
    }

    [Fact]
    public async Task AutoAssignCrewAsync_UsesLeftoverDrivers_AsHelpers()
    {
        var (evt, a, _) = await SeedPlanAsync();
        await RegisterForAreaAsync(evt, "Fahrer", FlomiDay, "anna", "beat");
        var service = CreateService();
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay });

        var result = await service.AutoAssignCrewAsync(evt.Id);

        Assert.Equal(new CrewAssignmentResult(1, 1), result);
        var day = DaysOf(evt.Id).Single();
        Assert.Equal(("anna", "beat"), (day.DriverUserId, day.HelperUserId));
    }

    [Fact]
    public async Task AutoAssignCrewAsync_KeepsExistingCrew_AndSkipsBusyUsers()
    {
        var (evt, a, b) = await SeedPlanAsync();
        await RegisterForAreaAsync(evt, "Fahrer", FlomiDay, "anna", "beat");
        var service = CreateService();
        // Anna fährt schon Arnold, Berger hat einen von Hand eingetragenen Fahrer
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay, DriverUserId = "anna" });
        await service.SaveAssignmentDayAsync(b.Id, evt.Id, new AssignmentDate { Date = FlomiDay, DriverName = "Onkel Max" });

        var result = await service.AutoAssignCrewAsync(evt.Id);

        // Kein Fahrer überschrieben; der übrige Beat springt beim ersten Fahrzeug als Helfer ein
        Assert.Equal(new CrewAssignmentResult(0, 1), result);
        var days = DaysOf(evt.Id);
        var arnold = days.Single(d => d.Assignment.VehicleId == a.Id);
        Assert.Equal(("anna", "beat"), (arnold.DriverUserId, arnold.HelperUserId));
        Assert.Null(days.Single(d => d.Assignment.VehicleId == b.Id).DriverUserId);
    }

    [Fact]
    public async Task AutoAssignCrewAsync_OnlyTouchesGivenDate_AndRegisteredDay()
    {
        var (evt, a, _) = await SeedPlanAsync();
        var dayBefore = FlomiDay.AddDays(-1);
        await RegisterForAreaAsync(evt, "Fahrer", FlomiDay, "anna");
        await RegisterForAreaAsync(evt, "Fahrer", dayBefore, "beat");
        var service = CreateService();
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay });
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = dayBefore });

        var result = await service.AutoAssignCrewAsync(evt.Id, dayBefore);

        Assert.Equal(1, result.DriversAssigned);
        var days = DaysOf(evt.Id);
        Assert.Equal("beat", days.Single(d => d.Date == dayBefore).DriverUserId);
        Assert.Null(days.Single(d => d.Date == FlomiDay).DriverUserId);
    }

    [Fact]
    public async Task CopyAssignmentsFromEventAsync_ShiftsDaysRelativeToEvent_WithoutCrew()
    {
        var (lastYear, a, b) = await SeedPlanAsync();
        int nextYearId, sampleId;
        using (var ctx = _db.CreateContext())
        {
            var nextYear = new Event { Name = "Flomi 2027", Date = new DateTime(2027, 11, 19) };
            var sample   = new Vehicle { OwnerName = "Beispiel", IsSample = true };
            ctx.Events.Add(nextYear);
            ctx.Vehicles.Add(sample);
            await ctx.SaveChangesAsync();
            (nextYearId, sampleId) = (nextYear.Id, sample.Id);
        }
        var service = CreateService();
        await service.SaveAssignmentDayAsync(a.Id, lastYear.Id, new AssignmentDate { Date = FlomiDay.AddDays(-1), DriverUserId = "anna", ReadyFrom = "08:00" });
        await service.SaveAssignmentDayAsync(a.Id, lastYear.Id, new AssignmentDate { Date = FlomiDay, HelperName = "Tim" });
        await service.SaveAssignmentDayAsync(sampleId, lastYear.Id, new AssignmentDate { Date = FlomiDay });
        // Berger ist im neuen Jahr schon eingeteilt und wird nicht doppelt übernommen
        await service.SaveAssignmentDayAsync(b.Id, lastYear.Id, new AssignmentDate { Date = FlomiDay });
        await service.SaveAssignmentDayAsync(b.Id, nextYearId, new AssignmentDate { Date = new DateTime(2027, 11, 19) });

        var copied = await service.CopyAssignmentsFromEventAsync(lastYear.Id, nextYearId);

        Assert.Equal(1, copied);
        var days = DaysOf(nextYearId).Where(d => d.Assignment.VehicleId == a.Id).OrderBy(d => d.Date).ToList();
        Assert.Equal(new[] { new DateTime(2027, 11, 18), new DateTime(2027, 11, 19) }, days.Select(d => d.Date));
        Assert.Equal("08:00", days[0].ReadyFrom);
        Assert.All(days, d => Assert.False(d.HasDriver || d.HasHelper));
        // Arnold (2 Tage) + Berger (schon vorhanden, 1 Tag) – das Beispiel-Fahrzeug nicht
        Assert.Equal(3, DaysOf(nextYearId).Count);
    }

    [Fact]
    public async Task GetDayCoverageAsync_CountsPickupsAndVehiclesWithoutDriver()
    {
        var (evt, a, b) = await SeedPlanAsync();
        var service = CreateService();
        await service.SaveAssignmentDayAsync(a.Id, evt.Id, new AssignmentDate { Date = FlomiDay, DriverName = "Anna" });
        await service.SaveAssignmentDayAsync(b.Id, evt.Id, new AssignmentDate { Date = FlomiDay });
        using (var ctx = _db.CreateContext())
        {
            ctx.FurniturePickupRequests.AddRange(
                new FurniturePickupRequest { FirstName = "X", LastName = "Y", Email = "x@example.com", Street = "S", City = "C", PostalCode = "6300", Description = "Tisch",
                                             PickupDate = FlomiDay.AddDays(-1), EventId = evt.Id, Status = PickupRequestStatus.Accepted },
                new FurniturePickupRequest { FirstName = "X", LastName = "Y", Email = "x@example.com", Street = "S", City = "C", PostalCode = "6300", Description = "Stuhl",
                                             PickupDate = FlomiDay.AddDays(-1), EventId = evt.Id, Status = PickupRequestStatus.Pending });
            await ctx.SaveChangesAsync();
        }

        var coverage = await service.GetDayCoverageAsync(new[] { evt.Id });

        var dayBefore = coverage.Single(c => c.Date == FlomiDay.AddDays(-1));
        Assert.Equal(1, dayBefore.Pickups);
        Assert.True(dayBefore.PickupsWithoutCrew);

        var flomi = coverage.Single(c => c.Date == FlomiDay);
        Assert.Equal((2, 1), (flomi.Vehicles, flomi.VehiclesWithDriver));
        Assert.True(flomi.NeedsAttention);
    }
}
