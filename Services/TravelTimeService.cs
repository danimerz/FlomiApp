// Services/TravelTimeService.cs
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FlomiApp.Services;

/// <summary>Fahrzeiten (Sekunden) und Distanzen (Meter) zwischen allen Punkten.</summary>
/// <param name="IsEstimate">true = aus Luftlinie geschätzt, nicht aus dem Strassennetz.</param>
public sealed record TravelMatrix(double[,] Seconds, double[,] Meters, bool IsEstimate);

public class RoutingOptions
{
    /// <summary>Gratis-Schlüssel von openrouteservice.org. Leer = Schätzung nach Luftlinie.</summary>
    public string? OpenRouteServiceApiKey { get; set; }
}

public interface ITravelTimeService
{
    Task<TravelMatrix> GetMatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default);
}

/// <summary>
/// Fahrzeiten über die Matrix-API von OpenRouteService (Strassennetz, PW-Profil), falls ein
/// Schlüssel hinterlegt ist; sonst – oder wenn der Dienst nicht antwortet – eine Schätzung
/// aus der Luftlinie. Übermittelt werden nur Koordinaten, keine Namen oder Adressen.
/// </summary>
public class TravelTimeService : ITravelTimeService
{
    private const string MatrixUrl = "https://api.openrouteservice.org/v2/matrix/driving-car";

    // Schätzung: Strassen sind im Ägerital gut 1/3 länger als die Luftlinie, Schnitt ~40 km/h
    public const double RoadFactor           = 1.35;
    public const double AverageMetersPerSec  = 40_000 / 3600.0;

    private readonly HttpClient _http;
    private readonly RoutingOptions _options;
    private readonly ILogger<TravelTimeService> _logger;

    public TravelTimeService(HttpClient http, IOptions<RoutingOptions> options, ILogger<TravelTimeService> logger)
    {
        _http    = http;
        _options = options.Value;
        _logger  = logger;
    }

    public async Task<TravelMatrix> GetMatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default)
    {
        var estimate = Estimate(points);
        if (string.IsNullOrWhiteSpace(_options.OpenRouteServiceApiKey) || points.Count < 2)
            return estimate;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, MatrixUrl)
            {
                Content = JsonContent.Create(new
                {
                    locations = points.Select(p => new[] { p.Longitude, p.Latitude }),
                    metrics   = new[] { "duration", "distance" },
                    units     = "m"
                })
            };
            request.Headers.TryAddWithoutValidation("Authorization", _options.OpenRouteServiceApiKey);

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var seconds = ReadMatrix(doc.RootElement.GetProperty("durations"), estimate.Seconds);
            var meters  = ReadMatrix(doc.RootElement.GetProperty("distances"), estimate.Meters);
            return new TravelMatrix(seconds, meters, IsEstimate: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "OpenRouteService nicht erreichbar – Fahrzeiten werden nach Luftlinie geschätzt.");
            return estimate;
        }
    }

    /// <summary>Schätzung aus der Luftlinie (Haversine) mit Umweg-Faktor und Durchschnittstempo.</summary>
    public static TravelMatrix Estimate(IReadOnlyList<GeoPoint> points)
    {
        var n = points.Count;
        var seconds = new double[n, n];
        var meters  = new double[n, n];
        for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
        {
            if (i == j) continue;
            meters[i, j]  = HaversineMeters(points[i], points[j]) * RoadFactor;
            seconds[i, j] = meters[i, j] / AverageMetersPerSec;
        }
        return new TravelMatrix(seconds, meters, IsEstimate: true);
    }

    public static double HaversineMeters(GeoPoint a, GeoPoint b)
    {
        const double earthRadius = 6_371_000;
        static double Rad(double deg) => deg * Math.PI / 180;

        var dLat = Rad(b.Latitude - a.Latitude);
        var dLon = Rad(b.Longitude - a.Longitude);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Rad(a.Latitude)) * Math.Cos(Rad(b.Latitude)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * earthRadius * Math.Asin(Math.Sqrt(h));
    }

    // Einzelne Paare ohne Route (null) mit der Schätzung auffüllen
    private static double[,] ReadMatrix(JsonElement rows, double[,] fallback)
    {
        var n = fallback.GetLength(0);
        var result = new double[n, n];
        var i = 0;
        foreach (var row in rows.EnumerateArray())
        {
            var j = 0;
            foreach (var cell in row.EnumerateArray())
            {
                result[i, j] = cell.ValueKind == JsonValueKind.Number ? cell.GetDouble() : fallback[i, j];
                j++;
            }
            i++;
        }
        if (i != n) throw new JsonException("Matrix hat die falsche Grösse.");
        return result;
    }
}
