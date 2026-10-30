using FlomiApp.Helpers;
using Microsoft.Extensions.Configuration;

namespace FlomiApp.Tests.Helpers;

public class DesignThemeTests
{
    [Theory]
    [InlineData(null, "standard")]
    [InlineData("", "standard")]
    [InlineData("gibtsnicht", "standard")]
    [InlineData("morgarten", "morgarten")]
    [InlineData(" Morgarten-Lager ", "morgarten-lager")]
    [InlineData("STANDARD", "standard")]
    public void Normalize_FallsBackToStandard(string? input, string expected)
    {
        Assert.Equal(expected, DesignTheme.Normalize(input));
    }

    [Fact]
    public void StylesheetPath_OnlyForNonStandardThemes()
    {
        Assert.Null(DesignTheme.StylesheetPath("standard"));
        Assert.Null(DesignTheme.StylesheetPath("unbekannt"));
        Assert.Equal("css/themes/morgarten.css", DesignTheme.StylesheetPath("Morgarten"));
        Assert.Equal("css/themes/morgarten-lager.css", DesignTheme.StylesheetPath("morgarten-lager"));
    }

    [Fact]
    public void FromConfiguration_ReadsDesignTheme()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Design:Theme"] = "morgarten" })
            .Build();

        Assert.Equal("morgarten", DesignTheme.FromConfiguration(config));
        Assert.Equal("#020659", DesignTheme.ThemeColor("morgarten"));
    }

    [Fact]
    public void EveryTheme_HasStylesheetFile()
    {
        var wwwroot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "wwwroot"));
        foreach (var theme in DesignTheme.All)
        {
            var path = DesignTheme.StylesheetPath(theme);
            if (path != null)
                Assert.True(File.Exists(Path.Combine(wwwroot, path)), $"Fehlt: wwwroot/{path}");
        }
    }
}
