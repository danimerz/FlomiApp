using System.Text.Json;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;

namespace FlomiApp.Tests.Helpers;

public class MarketEventHelperTests
{
    private static Event Market(DateTime? start = null, DateTime? end = null) => new()
    {
        Name         = "Flomi 2027",
        Date         = new DateTime(2027, 3, 13),
        LocationName = "Dreifachturnhalle",
        Street       = "Schulweg 1",
        PostalCode   = "6300",
        City         = "Zug",
        MarketStart  = start,
        MarketEnd    = end,
    };

    [Fact]
    public void FormatWhen_SameDay_ShowsTimeRange()
    {
        var evt = Market(new DateTime(2027, 3, 13, 8, 0, 0), new DateTime(2027, 3, 13, 15, 0, 0));
        Assert.Equal("Samstag, 13. März 2027, 08:00–15:00 Uhr", MarketEventHelper.FormatWhen(evt));
    }

    [Fact]
    public void FormatWhen_WithoutTimes_ShowsDateOnly()
    {
        Assert.Equal("Samstag, 13. März 2027", MarketEventHelper.FormatWhen(Market()));
    }

    [Fact]
    public void FormatWhen_MultiDay_ShowsBothDays()
    {
        var evt = Market(new DateTime(2027, 3, 12, 18, 0, 0), new DateTime(2027, 3, 13, 15, 0, 0));
        Assert.Equal("Freitag, 12. März, 18:00 – Samstag, 13. März 2027, 15:00 Uhr", MarketEventHelper.FormatWhen(evt));
    }

    [Fact]
    public void LocationText_SkipsEmptyParts()
    {
        Assert.Equal("Dreifachturnhalle, Schulweg 1, 6300 Zug", Market().LocationText);
        Assert.Equal("Zug", new Event { City = "Zug" }.LocationText);
    }

    [Fact]
    public void EventJsonLd_IsNull_WithoutLocation()
    {
        var evt = new Event { Name = "Flomi", Date = new DateTime(2027, 3, 13) };
        Assert.Null(MarketEventHelper.BuildEventJsonLd(evt, "https://pfadiflomi.ch", "img/x.jpg"));
    }

    [Fact]
    public void EventJsonLd_ContainsPlaceAndSwissTimes()
    {
        var evt  = Market(new DateTime(2027, 3, 13, 8, 0, 0), new DateTime(2027, 3, 13, 15, 0, 0));
        var json = MarketEventHelper.BuildEventJsonLd(evt, "https://pfadiflomi.ch", "img/x.jpg")!;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Event", root.GetProperty("@type").GetString());
        Assert.Equal("2027-03-13T08:00:00+01:00", root.GetProperty("startDate").GetString());
        Assert.Equal("2027-03-13T15:00:00+01:00", root.GetProperty("endDate").GetString());
        Assert.Equal("Dreifachturnhalle", root.GetProperty("location").GetProperty("name").GetString());
        Assert.Equal("6300", root.GetProperty("location").GetProperty("address").GetProperty("postalCode").GetString());
        Assert.Equal("https://pfadiflomi.ch/img/x.jpg", root.GetProperty("image")[0].GetString());
    }

    [Fact]
    public void FormatSchemaDate_UsesSummerTimeOffset()
    {
        Assert.Equal("2027-09-11T09:00:00+02:00",
            MarketEventHelper.FormatSchemaDate(new DateTime(2027, 9, 11, 9, 0, 0), default));
        Assert.Equal("2027-09-11", MarketEventHelper.FormatSchemaDate(null, new DateTime(2027, 9, 11)));
    }

    [Fact]
    public void NormalizeMarketTimes_MovesEndToStartDay_WhenOnlyTimeWasSet()
    {
        // Wie im Admin passiert: Ende mit Datum von heute, Uhrzeit passt zum Markttag
        var evt = Market(new DateTime(2027, 4, 17, 9, 0, 0), new DateTime(2026, 10, 30, 17, 0, 0));

        Assert.True(MarketEventHelper.NormalizeMarketTimes(evt));
        Assert.Equal(new DateTime(2027, 4, 17, 17, 0, 0), evt.MarketEnd);
    }

    [Fact]
    public void NormalizeMarketTimes_RejectsEndBeforeStartOnSameDay()
    {
        var evt = Market(new DateTime(2027, 4, 17, 9, 0, 0), new DateTime(2027, 4, 17, 8, 0, 0));
        Assert.False(MarketEventHelper.NormalizeMarketTimes(evt));
    }

    [Fact]
    public void EventJsonLd_NeverHasEndBeforeStart_AndDescribesEvent()
    {
        var evt  = Market(new DateTime(2027, 4, 17, 9, 0, 0), new DateTime(2027, 4, 17, 8, 0, 0));
        var json = MarketEventHelper.BuildEventJsonLd(evt, "https://pfadiflomi.ch", "img/x.jpg")!;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(root.GetProperty("startDate").GetString(), root.GetProperty("endDate").GetString());
        Assert.StartsWith("Flomi 2027: Flohmarkt der Pfadi Morgarten, Dreifachturnhalle", root.GetProperty("description").GetString());
    }
}
