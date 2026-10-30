using System.Globalization;
using System.Reflection;

namespace FlomiApp.Helpers;

/// <summary>
/// Laufende Version für den Footer. Beim Deploy setzt GitHub Actions die Build-Nummer
/// (<c>-p:Version=1.0.&lt;Lauf&gt;</c>), das SDK hängt den Git-Commit an («1.0.42+3f2a…»).
/// Release-Builds tragen zusätzlich den Build-Zeitpunkt (UTC) als Assembly-Metadaten «BuildTime».
/// </summary>
public static class AppVersion
{
    private static readonly Assembly App = typeof(AppVersion).Assembly;

    public static string Display { get; } =
        Format(App.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
               App.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildTime")?.Value);

    /// <summary>«1.0.42+3f2a9c1d…», «2026-10-30T13:35:00Z» → «1.0.42 (3f2a9c1) · 30.10.2026 14:35».</summary>
    public static string Format(string? informationalVersion, string? buildTimeUtc = null)
    {
        var text = FormatVersion(informationalVersion);

        if (DateTime.TryParse(buildTimeUtc, CultureInfo.InvariantCulture,
                              DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var utc))
            text += $" · {ToSwissTime(utc):dd.MM.yyyy HH:mm}";

        return text;
    }

    private static string FormatVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion)) return "unbekannt";

        var parts   = informationalVersion.Split('+', 2);
        var version = parts[0];
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1])) return version;

        var commit = parts[1].Length > 7 ? parts[1][..7] : parts[1];
        return $"{version} ({commit})";
    }

    private static DateTime ToSwissTime(DateTime utc)
    {
        try   { return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich")); }
        catch (TimeZoneNotFoundException) { return utc; }
        catch (InvalidTimeZoneException)  { return utc; }
    }
}
