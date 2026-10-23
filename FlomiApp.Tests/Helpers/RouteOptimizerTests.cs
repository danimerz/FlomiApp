using FlomiApp.Helpers;
using FlomiApp.Services;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class RouteOptimizerTests
{
    // Startort Unterägeri; Testpunkte als Abweichung in Grad (≈ 0.01° ≈ 1 km)
    private static readonly GeoPoint Depot = new(47.135, 8.575);

    private static double[,] Matrix(params (double dLat, double dLon)[] stops) =>
        TravelTimeService.Estimate(
            stops.Select(s => new GeoPoint(Depot.Latitude + s.dLat, Depot.Longitude + s.dLon))
                 .Prepend(Depot).ToList()).Seconds;

    [Fact]
    public void Distribute_VisitsEveryStopExactlyOnce()
    {
        var m = Matrix((0.01, 0), (0.02, 0.01), (-0.01, 0.02), (0, -0.03), (0.03, 0.03), (-0.02, -0.01), (0.01, 0.04));

        var plan = RouteOptimizer.Distribute(m, vehicleCount: 3);

        Assert.Equal(3, plan.Routes.Count);
        Assert.Equal(Enumerable.Range(1, 7), plan.Routes.SelectMany(r => r).OrderBy(s => s));
    }

    [Fact]
    public void Distribute_GivesEachVehicleOneOfTwoDistantClusters()
    {
        // Stopps 1-3 im Norden (Richtung Zug), 4-6 im Osten (Richtung Oberägeri/Morgarten)
        var m = Matrix((0.08, 0.00), (0.085, 0.005), (0.09, -0.005),
                       (0.00, 0.08), (0.005, 0.085), (-0.005, 0.09));

        var plan = RouteOptimizer.Distribute(m, vehicleCount: 2);

        var clusters = plan.Routes.Select(r => r.OrderBy(s => s).ToArray()).OrderBy(r => r.FirstOrDefault()).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, clusters[0]);
        Assert.Equal(new[] { 4, 5, 6 }, clusters[1]);
    }

    [Fact]
    public void Distribute_SingleVehicle_DrivesAlongTheLineInsteadOfZigzagging()
    {
        // Stopps in zufälliger Reihenfolge auf einer Linie nach Osten
        var m = Matrix((0, 0.04), (0, 0.01), (0, 0.03), (0, 0.02));

        var route = RouteOptimizer.Distribute(m, vehicleCount: 1).Routes[0];

        // Hin und zurück auf derselben Linie: aufsteigend oder absteigend, aber ohne Hin und Her
        var expected = new[] { 2, 4, 3, 1 };
        Assert.True(route.SequenceEqual(expected) || route.SequenceEqual(expected.Reverse()),
            $"Reihenfolge {string.Join(",", route)}");
    }

    [Fact]
    public void Distribute_SpreadsWorkSoNoVehicleIsIdleWhileAnotherDrivesAllDay()
    {
        // 8 Stopps rund um den Startort – mit 2 Fahrzeugen sollen beide etwas zu tun haben
        var m = Matrix((0.02, 0), (0.02, 0.02), (0, 0.02), (-0.02, 0.02),
                       (-0.02, 0), (-0.02, -0.02), (0, -0.02), (0.02, -0.02));

        var plan = RouteOptimizer.Distribute(m, vehicleCount: 2);

        Assert.All(plan.Routes, r => Assert.InRange(r.Count, 3, 5));
        Assert.True(plan.LongestSeconds < plan.RouteSeconds.Sum() * 0.65);
    }

    [Fact]
    public void Distribute_IsNeverWorseThanTheOldPostalCodeBlocks()
    {
        // Alte Logik: Liste in gleich grosse Blöcke schneiden, ohne auf die Lage zu achten
        var m = Matrix((0.05, 0), (-0.04, 0.03), (0.045, 0.01), (-0.03, 0.04), (0.01, -0.05), (0.02, -0.045));
        var naive = new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } };
        var naiveTotal = naive.Sum(r => RouteOptimizer.RouteSeconds(m, r));

        var plan = RouteOptimizer.Distribute(m, vehicleCount: 2);

        Assert.True(plan.RouteSeconds.Sum() <= naiveTotal + 1e-6);
    }

    [Fact]
    public void OrderOnly_KeepsAssignmentAndImprovesOrder()
    {
        var m = Matrix((0, 0.04), (0, 0.01), (0, 0.03), (0, 0.02), (0.05, 0));
        var assigned = new IReadOnlyList<int>[] { new[] { 1, 2, 3, 4 }, new[] { 5 } };

        var plan = RouteOptimizer.OrderOnly(m, assigned);

        Assert.Equal(new[] { 1, 2, 3, 4 }, plan.Routes[0].OrderBy(s => s));
        Assert.Equal(new[] { 5 }, plan.Routes[1]);
        Assert.True(plan.RouteSeconds[0] <= RouteOptimizer.RouteSeconds(m, assigned[0]));
    }

    [Fact]
    public void Distribute_WithoutStops_ReturnsEmptyRoutes()
    {
        var plan = RouteOptimizer.Distribute(Matrix(), vehicleCount: 2);

        Assert.All(plan.Routes, r => Assert.Empty(r));
        Assert.Equal(0, plan.LongestSeconds);
    }

    [Fact]
    public void RouteSeconds_IncludesReturnTripAndLoadingTime()
    {
        var m = new double[,] { { 0, 100 }, { 200, 0 } };

        Assert.Equal(100 + 200 + RouteOptimizer.ServiceSecondsPerStop, RouteOptimizer.RouteSeconds(m, new[] { 1 }));
    }
}
