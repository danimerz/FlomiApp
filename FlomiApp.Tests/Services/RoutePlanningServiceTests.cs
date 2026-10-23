using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlomiApp.Tests.Services;

public class RoutePlanningServiceTests : IDisposable
{
    private const string Start = "Gewerbestrasse 1, 6314 Unterägeri";
    private static readonly DateTime Day = new(2026, 11, 20);

    private readonly TestDb _db = new();
    private readonly FakeGeocoder _geocoder = new();

    public void Dispose() => _db.Dispose();

    private sealed class FakeGeocoder : IGeocodingService
    {
        public Dictionary<string, GeoPoint> Known { get; } = new()
        {
            [Start] = new(47.135, 8.575),
        };
        public List<string> Calls { get; } = new();

        public Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct = default)
        {
            Calls.Add(address);
            return Task.FromResult(Known.TryGetValue(address, out var p) ? new GeocodeResult(p, false) : null);
        }
    }

    private sealed class EstimateTravelTimes : ITravelTimeService
    {
        public Task<TravelMatrix> GetMatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default) =>
            Task.FromResult(TravelTimeService.Estimate(points));
    }

    private RoutePlanningService CreateService(Data.ApplicationDbContext ctx) =>
        new(ctx, _geocoder, new EstimateTravelTimes());

    private async Task<(int EventId, int[] VehicleIds)> SeedAsync(params (string Street, double Lat, double Lon)[] stops)
    {
        using var ctx = _db.CreateContext();
        var evt = new Event { Name = "Flomi", Date = Day.AddDays(1) };
        var v1 = new Vehicle { OwnerName = "Bus" };
        var v2 = new Vehicle { OwnerName = "Kombi" };
        ctx.AddRange(evt, v1, v2);
        await ctx.SaveChangesAsync();

        var order = 1000;
        foreach (var (street, lat, lon) in stops)
        {
            var request = new FurniturePickupRequest
            {
                OrderNumber = order++, FirstName = "Test", LastName = "Person", Email = "t@example.invalid",
                Street = street, PostalCode = "6314", City = "Unterägeri", Description = "Möbel",
                PickupDate = Day, Status = PickupRequestStatus.Accepted, EventId = evt.Id
            };
            ctx.FurniturePickupRequests.Add(request);
            if (!double.IsNaN(lat))
                _geocoder.Known[RoutePlanningService.AddressOf(request)] = new GeoPoint(lat, lon);
        }
        await ctx.SaveChangesAsync();
        return (evt.Id, new[] { v1.Id, v2.Id });
    }

    [Fact]
    public async Task Distribute_AssignsVehiclesAndStopOrder_AndStoresCoordinates()
    {
        var (eventId, vehicles) = await SeedAsync(
            ("Nord 1", 47.215, 8.575), ("Nord 2", 47.22, 8.58),
            ("Ost 1", 47.135, 8.655), ("Ost 2", 47.14, 8.66));

        RoutePlanningResult result;
        using (var ctx = _db.CreateContext())
            result = await CreateService(ctx).PlanAsync(eventId, Day, Start, vehicles, RoutePlanMode.Distribute);

        Assert.True(result.Success);
        Assert.True(result.IsEstimate);
        Assert.Equal(4, result.Vehicles.Sum(v => v.Stops));

        using var check = _db.CreateContext();
        var pickups = await check.FurniturePickupRequests.OrderBy(p => p.OrderNumber).ToListAsync();
        Assert.All(pickups, p =>
        {
            Assert.NotNull(p.AssignedVehicleId);
            Assert.NotNull(p.RouteStopOrder);
            Assert.NotNull(p.Latitude);
        });
        // Die beiden Nord-Stopps fahren zusammen, die beiden Ost-Stopps auch
        Assert.Equal(pickups[0].AssignedVehicleId, pickups[1].AssignedVehicleId);
        Assert.Equal(pickups[2].AssignedVehicleId, pickups[3].AssignedVehicleId);
        Assert.NotEqual(pickups[0].AssignedVehicleId, pickups[2].AssignedVehicleId);
    }

    [Fact]
    public async Task UnknownAddress_IsReportedAndLeftUnassigned()
    {
        var (eventId, vehicles) = await SeedAsync(("Bekannt 1", 47.14, 8.58), ("Phantasieweg 99", double.NaN, 0));

        RoutePlanningResult result;
        using (var ctx = _db.CreateContext())
            result = await CreateService(ctx).PlanAsync(eventId, Day, Start, vehicles, RoutePlanMode.Distribute);

        Assert.Equal(new[] { 1001 }, result.UnlocatedOrderNumbers);

        using var check = _db.CreateContext();
        var unknown = await check.FurniturePickupRequests.SingleAsync(p => p.OrderNumber == 1001);
        Assert.Null(unknown.AssignedVehicleId);
        Assert.Null(unknown.Latitude);
        Assert.NotNull(unknown.GeocodedAddress); // als «gesucht, nicht gefunden» markiert
    }

    [Fact]
    public async Task SecondRun_ReusesStoredCoordinates()
    {
        var (eventId, vehicles) = await SeedAsync(("A 1", 47.14, 8.58), ("B 2", 47.15, 8.59));

        using (var ctx = _db.CreateContext())
            await CreateService(ctx).PlanAsync(eventId, Day, Start, vehicles, RoutePlanMode.Distribute);
        _geocoder.Calls.Clear();
        using (var ctx = _db.CreateContext())
            await CreateService(ctx).PlanAsync(eventId, Day, Start, vehicles, RoutePlanMode.Distribute);

        Assert.Equal(new[] { Start }, _geocoder.Calls); // nur noch die Startadresse
    }

    [Fact]
    public async Task OrderOnly_KeepsManualAssignment()
    {
        var (eventId, vehicles) = await SeedAsync(("A 1", 47.14, 8.58), ("B 2", 47.15, 8.59), ("C 3", 47.16, 8.60));
        using (var ctx = _db.CreateContext())
        {
            // Alles von Hand dem zweiten Fahrzeug gegeben
            foreach (var p in ctx.FurniturePickupRequests) p.AssignedVehicleId = vehicles[1];
            await ctx.SaveChangesAsync();
        }

        using (var ctx = _db.CreateContext())
            await CreateService(ctx).PlanAsync(eventId, Day, Start, vehicles, RoutePlanMode.OrderOnly);

        using var check = _db.CreateContext();
        var pickups = await check.FurniturePickupRequests.ToListAsync();
        Assert.All(pickups, p => Assert.Equal(vehicles[1], p.AssignedVehicleId));
        Assert.Equal(new[] { 1, 2, 3 }, pickups.Select(p => p.RouteStopOrder!.Value).OrderBy(o => o));
    }

    [Fact]
    public async Task UnknownStartAddress_ReturnsErrorWithoutChanges()
    {
        var (eventId, vehicles) = await SeedAsync(("A 1", 47.14, 8.58));

        using var ctx = _db.CreateContext();
        var result = await CreateService(ctx).PlanAsync(eventId, Day, "Nirgendwo 1, 9999 Nirgends", vehicles, RoutePlanMode.Distribute);

        Assert.False(result.Success);
        Assert.Contains("Startadresse", result.Error);
        Assert.Null((await ctx.FurniturePickupRequests.SingleAsync()).AssignedVehicleId);
    }
}
