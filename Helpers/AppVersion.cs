using System.Reflection;

namespace FlomiApp.Helpers;

/// <summary>
/// Laufende Version für den Footer. Beim Deploy setzt GitHub Actions die Build-Nummer
/// (<c>-p:Version=1.0.&lt;Lauf&gt;</c>), das SDK hängt den Git-Commit an («1.0.42+3f2a…»).
/// </summary>
public static class AppVersion
{
    public static string Display { get; } =
        Format(typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>«1.0.42+3f2a9c1d…» → «1.0.42 (3f2a9c1)».</summary>
    public static string Format(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion)) return "unbekannt";

        var parts   = informationalVersion.Split('+', 2);
        var version = parts[0];
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1])) return version;

        var commit = parts[1].Length > 7 ? parts[1][..7] : parts[1];
        return $"{version} ({commit})";
    }
}
