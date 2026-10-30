using System.Text;
using System.Text.Json;

namespace FlomiApp.Helpers;

/// <summary>
/// Suchmaschinen-Angaben: Seitenname, öffentliche Seiten für die Sitemap, robots.txt.
/// Die Basis-URL kommt aus der Anfrage (hinter dem Proxy über ForwardedHeaders), damit
/// keine Domain fest im Code steht.
/// </summary>
public static class SeoHelper
{
    public const string SiteName = "Pfadi Flomi";

    public const string DefaultDescription =
        "Melde dich als Helfer/in für den Flohmarkt der Pfadi Morgarten an: Einsätze beim Sortieren, " +
        "Verkaufen und Aufräumen online buchen, Möbel zur Abholung anmelden und alle News zum Flomi lesen.";

    /// <summary>Vorschaubild für Google, WhatsApp und Co. (relativ zur Basis-URL).</summary>
    public const string DefaultImage = "img/home-hero-bg.jpg";

    public record PublicPage(string Path, string ChangeFrequency, string Priority);

    /// <summary>Seiten, die ohne Login erreichbar sind und gefunden werden sollen.</summary>
    public static readonly IReadOnlyList<PublicPage> PublicPages =
    [
        new("",                          "weekly",  "1.0"),
        new("news",                      "weekly",  "0.8"),
        new("furniture-pickup",          "monthly", "0.8"),
        new("Identity/Account/Register", "yearly",  "0.5"),
        new("Identity/Account/Login",    "yearly",  "0.3"),
        new("impressum",                 "yearly",  "0.1"),
        new("datenschutz",               "yearly",  "0.1"),
    ];

    /// <summary>«Titel – Pfadi Flomi»; ohne Titel nur der Seitenname.</summary>
    public static string FullTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? SiteName : $"{title} – {SiteName}";

    /// <summary>Basis-URL immer mit abschliessendem «/».</summary>
    public static string NormalizeBaseUrl(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    public static string BuildRobotsTxt(string baseUrl)
    {
        var b = NormalizeBaseUrl(baseUrl);
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /Identity/Account/Register");
        sb.AppendLine("Allow: /Identity/Account/Login");
        sb.AppendLine("Disallow: /admin");
        sb.AppendLine("Disallow: /user");
        sb.AppendLine("Disallow: /Identity/");
        sb.AppendLine("Disallow: /pickup-images/");
        sb.AppendLine("Disallow: /_blazor");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {b}sitemap.xml");
        return sb.ToString();
    }

    public static string BuildSitemap(string baseUrl)
    {
        var b = NormalizeBaseUrl(baseUrl);
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");
        foreach (var page in PublicPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(b + page.Path)}</loc>");
            sb.AppendLine($"    <changefreq>{page.ChangeFrequency}</changefreq>");
            sb.AppendLine($"    <priority>{page.Priority}</priority>");
            sb.AppendLine("  </url>");
        }
        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    /// <summary>
    /// Strukturierte Daten (schema.org WebSite), damit Google «Pfadi Flomi» als Seitennamen
    /// anzeigt statt nur die Domain. «&lt;» wird escaped, damit der Inhalt das Script-Tag nicht schliesst.
    /// </summary>
    public static string BuildWebSiteJsonLd(string baseUrl)
    {
        var data = new Dictionary<string, object>
        {
            ["@context"]      = "https://schema.org",
            ["@type"]         = "WebSite",
            ["name"]          = SiteName,
            ["alternateName"] = new[] { "Flomi App", "Pfadi-Flohmarkt Morgarten" },
            ["url"]           = NormalizeBaseUrl(baseUrl),
            ["inLanguage"]    = "de-CH",
            ["description"]   = DefaultDescription,
        };
        return JsonSerializer.Serialize(data).Replace("<", "\\u003c");
    }
}
