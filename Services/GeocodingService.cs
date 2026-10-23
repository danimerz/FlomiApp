// Services/GeocodingService.cs
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlomiApp.Services;

/// <summary>Koordinate in WGS84 (wie GPS / Google Maps).</summary>
public sealed record GeoPoint(double Latitude, double Longitude);

/// <param name="Approximate">Strasse/Hausnummer nicht exakt gefunden – Position nur ungefähr (gleiche PLZ).</param>
public sealed record GeocodeResult(GeoPoint Point, bool Approximate);

public interface IGeocodingService
{
    /// <summary>Sucht eine Schweizer Adresse («Strasse Nr, PLZ Ort»). Null wenn nicht gefunden.</summary>
    Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct = default);
}

/// <summary>
/// Adresssuche über die Such-API von geo.admin.ch (Bund, amtliches Gebäudeadressverzeichnis).
/// Gratis, ohne Schlüssel. Die Suche ist unscharf und findet sonst gern die gleiche
/// Strasse in einem anderen Kanton – deshalb muss die Postleitzahl übereinstimmen.
/// </summary>
public partial class GeoAdminGeocodingService : IGeocodingService
{
    private const string SearchUrl = "https://api3.geo.admin.ch/rest/services/api/SearchServer";

    private readonly HttpClient _http;
    private readonly ILogger<GeoAdminGeocodingService> _logger;

    public GeoAdminGeocodingService(HttpClient http, ILogger<GeoAdminGeocodingService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<GeocodeResult?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var url = $"{SearchUrl}?type=locations&origins=address&sr=4326&limit=5&searchText={Uri.EscapeDataString(address.Trim())}";
        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var candidates = doc.RootElement.GetProperty("results").EnumerateArray()
                .Select(r => r.GetProperty("attrs"))
                .Select(a => (Label: StripTags(a.GetProperty("label").GetString() ?? ""),
                              Lat: a.GetProperty("lat").GetDouble(),
                              Lon: a.GetProperty("lon").GetDouble()));

            return PickBest(address, candidates);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Adresssuche bei geo.admin.ch fehlgeschlagen.");
            return null;
        }
    }

    /// <summary>
    /// Wählt den ersten Treffer mit passender Postleitzahl. Stimmt auch «Strasse Nr» überein,
    /// gilt die Position als exakt, sonst als ungefähr.
    /// </summary>
    public static GeocodeResult? PickBest(string address, IEnumerable<(string Label, double Lat, double Lon)> candidates)
    {
        var postalCode = PostalCodeRegex().Match(address).Value;
        var streetPart = Normalize(address.Split(',')[0]);

        foreach (var (label, lat, lon) in candidates)
        {
            if (postalCode.Length > 0 && !PostalCodeRegex().Matches(label).Any(m => m.Value == postalCode))
                continue;

            var exact = streetPart.Length > 0 && Normalize(label).StartsWith(streetPart, StringComparison.Ordinal);
            return new GeocodeResult(new GeoPoint(lat, lon), Approximate: !exact);
        }
        return null;
    }

    private static string StripTags(string label) => TagRegex().Replace(label, "");

    // «Zugerstr. 10» und «Zugerstrasse 10» gelten als gleich; Leerzeichen/Satzzeichen zählen nicht.
    private static string Normalize(string text) =>
        new string(text.ToLower(CultureInfo.GetCultureInfo("de-CH"))
            .Replace("str.", "strasse")
            .Where(char.IsLetterOrDigit).ToArray());

    [GeneratedRegex(@"\b\d{4}\b")]
    private static partial Regex PostalCodeRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
