// Services/RoutePlanningService.cs
using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public enum RoutePlanMode
{
    /// <summary>Abholungen neu auf die Fahrzeuge verteilen und Reihenfolge festlegen.</summary>
    Distribute,
    /// <summary>Zuteilung behalten, nur die Reihenfolge pro Fahrzeug optimieren.</summary>
    OrderOnly
}

public sealed record VehicleRouteSummary(int VehicleId, int Stops, double Seconds, double Meters);

public sealed record RoutePlanningResult(
    string? Error,
    IReadOnlyList<VehicleRouteSummary> Vehicles,
    IReadOnlyList<int> UnlocatedOrderNumbers,
    IReadOnlyList<int> ApproximateOrderNumbers,
    bool IsEstimate)
{
    public bool Success => Error == null;

    public static RoutePlanningResult Failed(string error) => new(error, [], [], [], false);
}

public interface IRoutePlanningService
{
    /// <summary>
    /// Plant die Touren für alle akzeptierten Abholungen eines Tages und speichert
    /// Fahrzeug und Stopp-Reihenfolge. Adressen ohne Koordinaten bleiben ungeplant.
    /// </summary>
    Task<RoutePlanningResult> PlanAsync(int? eventId, DateTime date, string startAddress,
        IReadOnlyList<int> vehicleIds, RoutePlanMode mode, CancellationToken ct = default);
}

public class RoutePlanningService : IRoutePlanningService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeocodingService    _geocoding;
    private readonly ITravelTimeService   _travelTimes;

    public RoutePlanningService(ApplicationDbContext context, IGeocodingService geocoding, ITravelTimeService travelTimes)
    {
        _context     = context;
        _geocoding   = geocoding;
        _travelTimes = travelTimes;
    }

    public static string AddressOf(FurniturePickupRequest r) => $"{r.Street}, {r.PostalCode} {r.City}".Trim();

    public async Task<RoutePlanningResult> PlanAsync(int? eventId, DateTime date, string startAddress,
        IReadOnlyList<int> vehicleIds, RoutePlanMode mode, CancellationToken ct = default)
    {
        if (vehicleIds.Count == 0)
            return RoutePlanningResult.Failed("Für diesen Tag sind keine Fahrzeuge zugewiesen.");

        var start = await _geocoding.GeocodeAsync(startAddress, ct);
        if (start == null)
            return RoutePlanningResult.Failed($"Die Startadresse «{startAddress}» wurde nicht gefunden. Bitte als «Strasse Nr, PLZ Ort» eingeben.");

        var pickups = await _context.FurniturePickupRequests
            .Where(r => r.Status == PickupRequestStatus.Accepted
                     && r.PickupDate.Date == date.Date
                     && (eventId == null || r.EventId == eventId))
            .OrderBy(r => r.OrderNumber)
            .ToListAsync(ct);

        // ── Koordinaten ermitteln (nur für neue oder geänderte Adressen) ─────
        var approximate = new List<int>();
        foreach (var pickup in pickups)
        {
            var address = AddressOf(pickup);
            if (pickup.GeocodedAddress == address) continue;

            var result = await _geocoding.GeocodeAsync(address, ct);
            pickup.Latitude        = result?.Point.Latitude;
            pickup.Longitude       = result?.Point.Longitude;
            pickup.GeocodedAddress = address;
            if (result?.Approximate == true) approximate.Add(pickup.OrderNumber);
        }

        var located   = pickups.Where(p => p.Latitude.HasValue && p.Longitude.HasValue).ToList();
        var unlocated = pickups.Except(located).ToList();

        // ── Fahrzeiten + Optimierung ─────────────────────────────────────────
        // Matrix-Index 0 = Startort, 1..n = located[0..n-1]
        var points = located.Select(p => new GeoPoint(p.Latitude!.Value, p.Longitude!.Value)).Prepend(start.Point).ToList();
        var matrix = await _travelTimes.GetMatrixAsync(points, ct);

        RoutePlan plan;
        if (mode == RoutePlanMode.Distribute)
        {
            plan = RouteOptimizer.Distribute(matrix.Seconds, vehicleIds.Count);

            // Ohne Koordinaten lässt sich nichts sinnvoll zuteilen
            foreach (var p in unlocated)
            {
                p.AssignedVehicleId = null;
                p.RouteStopOrder    = null;
            }
        }
        else
        {
            var assigned = vehicleIds
                .Select(vid => (IReadOnlyList<int>)located
                    .Select((p, idx) => (p, Index: idx + 1))
                    .Where(x => x.p.AssignedVehicleId == vid)
                    .OrderBy(x => x.p.RouteStopOrder ?? int.MaxValue)
                    .Select(x => x.Index)
                    .ToList())
                .ToList();
            plan = RouteOptimizer.OrderOnly(matrix.Seconds, assigned);

            // Nicht lokalisierte Stopps ans Tourende (Reihenfolge unbekannt)
            foreach (var p in unlocated.Where(p => p.AssignedVehicleId.HasValue))
                p.RouteStopOrder = null;
        }

        var summaries = new List<VehicleRouteSummary>();
        for (var v = 0; v < vehicleIds.Count; v++)
        {
            var route = plan.Routes[v];
            for (var k = 0; k < route.Count; k++)
            {
                var pickup = located[route[k] - 1];
                pickup.AssignedVehicleId = vehicleIds[v];
                pickup.RouteStopOrder    = k + 1;
            }
            summaries.Add(new VehicleRouteSummary(vehicleIds[v], route.Count,
                plan.RouteSeconds[v], RouteOptimizer.RouteLength(matrix.Meters, route)));
        }

        await _context.SaveChangesAsync(ct);

        return new RoutePlanningResult(null, summaries,
            unlocated.Select(p => p.OrderNumber).ToList(), approximate, matrix.IsEstimate);
    }
}
