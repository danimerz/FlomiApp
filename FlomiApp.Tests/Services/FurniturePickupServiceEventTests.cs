using FlomiApp.Data.Models;
using FlomiApp.Services;
using FlomiApp.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlomiApp.Tests.Services;

public class FurniturePickupServiceEventTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetPickupEnabledEventsAsync_OnlyActiveEventsWithEnabledService()
    {
        using (var ctx = _db.CreateContext())
        {
            var enabled  = new Event { Name = "Mit Abholung",  Date = new DateTime(2027, 4, 17) };
            var disabled = new Event { Name = "Ohne Abholung", Date = new DateTime(2027, 9, 11) };
            var inactive = new Event { Name = "Inaktiv",       Date = new DateTime(2027, 5, 1), IsActive = false };
            var none     = new Event { Name = "Keine Settings", Date = new DateTime(2027, 6, 1) };
            ctx.Events.AddRange(enabled, disabled, inactive, none);
            await ctx.SaveChangesAsync();

            ctx.FurniturePickupSettings.AddRange(
                new FurniturePickupSettings { EventId = enabled.Id,  IsEnabled = true },
                new FurniturePickupSettings { EventId = disabled.Id, IsEnabled = false },
                new FurniturePickupSettings { EventId = inactive.Id, IsEnabled = true });
            await ctx.SaveChangesAsync();
        }

        using var context = _db.CreateContext();
        var service = new FurniturePickupService(context, new NoOpMailQueue(), NullLogger<FurniturePickupService>.Instance);

        var events = await service.GetPickupEnabledEventsAsync();

        Assert.Equal(new[] { "Mit Abholung" }, events.Select(e => e.Name));
    }
}
