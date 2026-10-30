using FlomiApp.Data.Models;
using FlomiApp.Services;

namespace FlomiApp.Tests.Services;

public class AreaServiceEventTests
{
    private static readonly DateTime Now = new(2027, 3, 13, 10, 0, 0);

    [Fact]
    public async Task GetNextPublicEventAsync_SkipsInactiveSampleAndPast()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Events.AddRange(
                new Event { Name = "Vorjahr",   Date = new DateTime(2026, 3, 14) },
                new Event { Name = "Inaktiv",   Date = new DateTime(2027, 3, 20), IsActive = false },
                new Event { Name = "Beispiel",  Date = new DateTime(2027, 3, 21), IsSample = true },
                new Event { Name = "Herbst",    Date = new DateTime(2027, 9, 11) },
                new Event { Name = "Frühling",  Date = new DateTime(2027, 4, 10) });
            await ctx.SaveChangesAsync();
        }

        using var context = db.CreateContext();
        var next = await new AreaService(context).GetNextPublicEventAsync(Now);

        Assert.Equal("Frühling", next?.Name);
    }

    [Fact]
    public async Task GetNextPublicEventAsync_ShowsMarketDay_UntilClosingTime()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Events.Add(new Event
            {
                Name        = "Heute",
                Date        = new DateTime(2027, 3, 13),
                MarketStart = new DateTime(2027, 3, 13, 8, 0, 0),
                MarketEnd   = new DateTime(2027, 3, 13, 15, 0, 0),
            });
            ctx.Events.Add(new Event { Name = "Später", Date = new DateTime(2027, 9, 11) });
            await ctx.SaveChangesAsync();
        }

        using var context = db.CreateContext();
        var service = new AreaService(context);

        Assert.Equal("Heute",  (await service.GetNextPublicEventAsync(Now))?.Name);
        Assert.Equal("Später", (await service.GetNextPublicEventAsync(Now.AddHours(6)))?.Name);
    }

    [Fact]
    public async Task UpdateEventAsync_SavesLocationAndOpeningTimes()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var evt = new Event { Name = "Flomi", Date = new DateTime(2027, 3, 13) };
            ctx.Events.Add(evt);
            await ctx.SaveChangesAsync();
            id = evt.Id;
        }

        using (var ctx = db.CreateContext())
        {
            await new AreaService(ctx).UpdateEventAsync(new Event
            {
                Id = id, Name = "Flomi", Date = new DateTime(2027, 3, 13), IsActive = true,
                LocationName = "Turnhalle", Street = "Weg 1", PostalCode = "6300", City = "Zug",
                MarketStart = new DateTime(2027, 3, 13, 8, 0, 0), MarketEnd = new DateTime(2027, 3, 13, 15, 0, 0),
            });
        }

        using var check = db.CreateContext();
        var saved = await new AreaService(check).GetEventByIdAsync(id);
        Assert.Equal("Turnhalle, Weg 1, 6300 Zug", saved.LocationText);
        Assert.Equal(new DateTime(2027, 3, 13, 15, 0, 0), saved.MarketEnd);
    }

    [Fact]
    public async Task GetNextPublicEventAsync_FixesEndDateEnteredOnWrongDay()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Events.Add(new Event
            {
                Name        = "Flomi 2027",
                Date        = new DateTime(2027, 4, 17),
                MarketStart = new DateTime(2027, 4, 17, 9, 0, 0),
                MarketEnd   = new DateTime(2026, 10, 30, 17, 0, 0),
            });
            await ctx.SaveChangesAsync();
        }

        using var context = db.CreateContext();
        var next = await new AreaService(context).GetNextPublicEventAsync(new DateTime(2026, 10, 30, 18, 0, 0));

        Assert.Equal(new DateTime(2027, 4, 17, 17, 0, 0), next?.MarketEnd);
    }
}
