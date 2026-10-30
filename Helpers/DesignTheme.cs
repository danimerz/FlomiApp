namespace FlomiApp.Helpers;

/// <summary>
/// Auswählbares Erscheinungsbild der ganzen Installation. Gesetzt über die Konfiguration
/// <c>Design:Theme</c>, auf dem Server z. B. als Umgebungsvariable <c>Design__Theme=morgarten</c>.
/// Hell-/Dunkelmodus der Benutzer funktioniert in jedem Theme.
/// </summary>
public static class DesignTheme
{
    public const string Standard       = "standard";
    public const string Morgarten      = "morgarten";
    public const string MorgartenLager = "morgarten-lager";

    // Farbe der Browser-Leiste (meta theme-color) je Theme
    private static readonly Dictionary<string, string> ThemeColors = new(StringComparer.OrdinalIgnoreCase)
    {
        [Standard]       = "#2563eb",
        [Morgarten]      = "#020659",
        [MorgartenLager] = "#1b1b1b",
    };

    public static IReadOnlyCollection<string> All => ThemeColors.Keys;

    /// <summary>Unbekannte oder leere Werte fallen auf «standard» zurück.</summary>
    public static string Normalize(string? value)
    {
        var key = value?.Trim().ToLowerInvariant();
        return key != null && ThemeColors.ContainsKey(key) ? key : Standard;
    }

    public static string FromConfiguration(IConfiguration configuration) =>
        Normalize(configuration["Design:Theme"]);

    public static string ThemeColor(string? theme) => ThemeColors[Normalize(theme)];

    /// <summary>Zusätzliches Stylesheet unter wwwroot; «standard» braucht keins.</summary>
    public static string? StylesheetPath(string? theme)
    {
        var key = Normalize(theme);
        return key == Standard ? null : $"css/themes/{key}.css";
    }
}
