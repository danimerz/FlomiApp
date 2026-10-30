using System.Globalization;
using System.Text.Json;
using FlomiApp.Data.Models;

namespace FlomiApp.Helpers;

/// <summary>
/// Öffentliche Angaben zum nächsten Markt: Anzeige auf der Startseite und
/// strukturierte Daten (schema.org Event), damit Google den Flohmarkt als Veranstaltung zeigt.
/// </summary>
public static class MarketEventHelper
{
    private static readonly CultureInfo De = new("de-CH");

    private static readonly TimeZoneInfo? Zurich = FindZurich();

    /// <summary>
    /// «Samstag, 13. März 2027, 08:00–15:00 Uhr», mehrtägig «Freitag, 12. März, 18:00 – Samstag, 13. März 2027, 15:00 Uhr»,
    /// ohne Öffnungszeiten nur das Datum.
    /// </summary>
    public static string FormatWhen(Event evt)
    {
        if (evt.MarketStart is not { } start)
            return evt.Date.ToString("dddd, d. MMMM yyyy", De);

        if (evt.MarketEnd is not { } end || end <= start)
            return start.ToString("dddd, d. MMMM yyyy, 'ab' HH:mm 'Uhr'", De);

        if (start.Date == end.Date)
            return $"{start.ToString("dddd, d. MMMM yyyy, HH:mm", De)}–{end.ToString("HH:mm", De)} Uhr";

        return $"{start.ToString("dddd, d. MMMM, HH:mm", De)} – {end.ToString("dddd, d. MMMM yyyy, HH:mm", De)} Uhr";
    }

    /// <summary>
    /// Korrigiert ein «geöffnet bis», bei dem nur die Uhrzeit gesetzt wurde und das Datum noch auf
    /// einem anderen Tag steht: liegt das Ende vor dem Start, passt aber die Uhrzeit am Starttag,
    /// wird es auf den Starttag gesetzt. Gibt true zurück, wenn die Zeiten danach gültig sind.
    /// </summary>
    public static bool NormalizeMarketTimes(Event evt)
    {
        if (evt.MarketStart is not { } start || evt.MarketEnd is not { } end) return true;
        if (end > start) return true;

        var sameDay = start.Date + end.TimeOfDay;
        if (sameDay > start)
        {
            evt.MarketEnd = sameDay;
            return true;
        }
        return false;
    }

    /// <summary>Beschreibung für Google, wenn beim Event keine hinterlegt ist.</summary>
    public static string DefaultEventDescription(Event evt) =>
        evt.HasLocation
            ? $"{evt.Name}: Flohmarkt der Pfadi Morgarten, {evt.LocationText}. {FormatWhen(evt)}. Eintritt frei."
            : $"{evt.Name}: Flohmarkt der Pfadi Morgarten. {FormatWhen(evt)}. Eintritt frei.";

    /// <summary>Aktuelle Schweizer Ortszeit, auch wenn der Server auf UTC läuft.</summary>
    public static DateTime SwissNow() =>
        Zurich != null ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zurich) : DateTime.Now;

    public static string? MapsUrl(Event evt) =>
        evt.HasLocation
            ? "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(evt.LocationText)
            : null;

    /// <summary>
    /// schema.org Event als JSON. Null, wenn kein Ort hinterlegt ist – ohne Ort zeigt Google keine Veranstaltung.
    /// </summary>
    public static string? BuildEventJsonLd(Event evt, string baseUrl, string imagePath)
    {
        if (!evt.HasLocation) return null;

        var b = SeoHelper.NormalizeBaseUrl(baseUrl);

        var address = new Dictionary<string, object> { ["@type"] = "PostalAddress", ["addressCountry"] = "CH" };
        if (!string.IsNullOrWhiteSpace(evt.Street))     address["streetAddress"]   = evt.Street.Trim();
        if (!string.IsNullOrWhiteSpace(evt.PostalCode)) address["postalCode"]      = evt.PostalCode.Trim();
        if (!string.IsNullOrWhiteSpace(evt.City))       address["addressLocality"] = evt.City.Trim();

        var data = new Dictionary<string, object>
        {
            ["@context"]            = "https://schema.org",
            ["@type"]               = "Event",
            ["name"]                = evt.Name,
            ["description"]         = string.IsNullOrWhiteSpace(evt.Description) ? DefaultEventDescription(evt) : evt.Description.Trim(),
            ["startDate"]           = FormatSchemaDate(evt.MarketStart, evt.Date),
            ["endDate"]             = FormatSchemaDate(evt.MarketEnd > evt.MarketStart ? evt.MarketEnd : evt.MarketStart, evt.Date),
            ["eventStatus"]         = "https://schema.org/EventScheduled",
            ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
            // Eintritt zum Flohmarkt ist gratis (steht auch sichtbar auf der Startseite)
            ["isAccessibleForFree"] = true,
            ["offers"] = new Dictionary<string, object>
            {
                ["@type"]         = "Offer",
                ["price"]         = "0",
                ["priceCurrency"] = "CHF",
                ["availability"]  = "https://schema.org/InStock",
                ["url"]           = b,
            },
            ["location"] = new Dictionary<string, object>
            {
                ["@type"]   = "Place",
                ["name"]    = string.IsNullOrWhiteSpace(evt.LocationName) ? evt.City!.Trim() : evt.LocationName.Trim(),
                ["address"] = address,
            },
            ["image"]     = new[] { b + imagePath },
            ["url"]       = b,
            ["organizer"] = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"]  = "Pfadi Morgarten",
                ["url"]   = "https://www.pfadimorgarten.ch",
            },
        };

        return JsonSerializer.Serialize(data).Replace("<", "\\u003c");
    }

    /// <summary>Mit Uhrzeit: ISO 8601 inkl. Schweizer Zeitzone; ohne: nur das Datum.</summary>
    public static string FormatSchemaDate(DateTime? time, DateTime fallbackDate)
    {
        if (time is not { } t)
            return fallbackDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var local = DateTime.SpecifyKind(t, DateTimeKind.Unspecified);
        return Zurich != null
            ? new DateTimeOffset(local, Zurich.GetUtcOffset(local)).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)
            : local.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo? FindZurich()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich"); }
        catch (TimeZoneNotFoundException) { return null; }
        catch (InvalidTimeZoneException) { return null; }
    }
}
