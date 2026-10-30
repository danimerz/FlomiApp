using System.Text.Json;
using System.Xml.Linq;
using FlomiApp.Helpers;

namespace FlomiApp.Tests.Helpers;

public class SeoHelperTests
{
    [Fact]
    public void RobotsTxt_BlocksPrivateAreas_AndPointsToSitemap()
    {
        var robots = SeoHelper.BuildRobotsTxt("https://pfadiflomi.ch");

        Assert.Contains("Disallow: /admin", robots);
        Assert.Contains("Disallow: /user", robots);
        Assert.Contains("Allow: /Identity/Account/Register", robots);
        Assert.Contains("Sitemap: https://pfadiflomi.ch/sitemap.xml", robots);
    }

    [Fact]
    public void Sitemap_IsValidXml_WithAllPublicPages()
    {
        var xml = XDocument.Parse(SeoHelper.BuildSitemap("https://pfadiflomi.ch/"));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var locs = xml.Descendants(ns + "loc").Select(e => e.Value).ToList();

        Assert.Equal(SeoHelper.PublicPages.Count, locs.Count);
        Assert.Contains("https://pfadiflomi.ch/", locs);
        Assert.Contains("https://pfadiflomi.ch/furniture-pickup", locs);
        Assert.DoesNotContain(locs, l => l.Contains("/admin") || l.Contains("/user/"));
    }

    [Theory]
    [InlineData(null, "Pfadi Flomi")]
    [InlineData("", "Pfadi Flomi")]
    [InlineData("News", "News – Pfadi Flomi")]
    public void FullTitle_AppendsSiteName(string? title, string expected)
    {
        Assert.Equal(expected, SeoHelper.FullTitle(title));
    }

    [Fact]
    public void WebSiteJsonLd_IsValidJson_AndCannotCloseScriptTag()
    {
        var json = SeoHelper.BuildWebSiteJsonLd("https://pfadiflomi.ch");

        Assert.DoesNotContain("<", json);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("WebSite", doc.RootElement.GetProperty("@type").GetString());
        Assert.Equal("https://pfadiflomi.ch/", doc.RootElement.GetProperty("url").GetString());
    }
}
