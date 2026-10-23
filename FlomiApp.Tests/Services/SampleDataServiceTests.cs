using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlomiApp.Tests.Services;

public class SampleDataServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_AddsHiddenEventWithVehiclesAndAcceptedPickups()
    {
        using var ctx = _db.CreateContext();
        var (eventId, pickupDate) = await new SampleDataService(ctx).CreateRouteSampleAsync();

        var evt = await ctx.Events.SingleAsync(e => e.Id == eventId);
        Assert.True(evt.IsSample);
        Assert.False(evt.IsActive); // Freiwillige sehen ihn nicht
        Assert.Equal(DayOfWeek.Friday, pickupDate.DayOfWeek);

        Assert.Equal(3, await ctx.Vehicles.CountAsync(v => v.IsSample));
        Assert.Equal(3, await ctx.AssignmentDates.CountAsync(d => d.Date == pickupDate));

        var pickups = await ctx.FurniturePickupRequests.Where(r => r.EventId == eventId).ToListAsync();
        Assert.Equal(19, pickups.Count);
        Assert.All(pickups, p =>
        {
            Assert.Equal(PickupRequestStatus.Accepted, p.Status);
            Assert.Equal(pickupDate, p.PickupDate);
            Assert.EndsWith("@example.invalid", p.Email);
        });

        var settings = await ctx.FurniturePickupSettings.SingleAsync(s => s.EventId == eventId);
        Assert.False(settings.IsEnabled);
        Assert.Equal(SampleDataService.SampleStartAddress, settings.DepartureAddress);
    }

    [Fact]
    public async Task CreateTwice_ReplacesInsteadOfDuplicating()
    {
        using (var ctx = _db.CreateContext())
            await new SampleDataService(ctx).CreateRouteSampleAsync();
        using (var ctx = _db.CreateContext())
            await new SampleDataService(ctx).CreateRouteSampleAsync();

        using var check = _db.CreateContext();
        Assert.Equal(1, await check.Events.CountAsync(e => e.IsSample));
        Assert.Equal(3, await check.Vehicles.CountAsync(v => v.IsSample));
        Assert.Equal(19, await check.FurniturePickupRequests.CountAsync());
    }

    [Fact]
    public async Task Remove_DeletesOnlySampleData()
    {
        using (var ctx = _db.CreateContext())
        {
            var realEvent = new Event { Name = "Flomi 2027", Date = new DateTime(2027, 4, 17) };
            ctx.Events.Add(realEvent);
            ctx.Vehicles.Add(new Vehicle { OwnerName = "Echter Bus" });
            await ctx.SaveChangesAsync();
            ctx.FurniturePickupRequests.Add(new FurniturePickupRequest
            {
                OrderNumber = 1000, FirstName = "Echt", LastName = "Person", Email = "echt@example.ch",
                Street = "Weg 1", PostalCode = "6314", City = "Unterägeri", Description = "Sofa",
                EventId = realEvent.Id
            });
            await ctx.SaveChangesAsync();

            var service = new SampleDataService(ctx);
            await service.CreateRouteSampleAsync();
            Assert.True(await service.HasRouteSampleAsync());
        }

        using (var ctx = _db.CreateContext())
            await new SampleDataService(ctx).RemoveRouteSampleAsync();

        using var check = _db.CreateContext();
        Assert.False(await new SampleDataService(check).HasRouteSampleAsync());
        Assert.Equal("Flomi 2027", (await check.Events.SingleAsync()).Name);
        Assert.Equal("Echter Bus", (await check.Vehicles.SingleAsync()).OwnerName);
        Assert.Equal(1000, (await check.FurniturePickupRequests.SingleAsync()).OrderNumber);
        Assert.Empty(await check.VehicleAssignments.ToListAsync());
        Assert.Empty(await check.FurniturePickupSettings.Where(s => s.EventId != null).ToListAsync());
    }
}
