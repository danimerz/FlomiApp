using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using FlomiApp.Services;
using FlomiApp.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlomiApp.Tests.Services;

public class FurniturePickupServiceImageTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static FurniturePickupService CreateService(Data.ApplicationDbContext ctx) =>
        new(ctx, new NoOpMailQueue(), NullLogger<FurniturePickupService>.Instance);

    private static FurniturePickupRequest NewRequest(int? eventId = null, DateTime? pickupDate = null) => new()
    {
        FirstName = "Max", LastName = "Muster", Email = "max@example.ch",
        Street = "Weg 1", City = "Zug", PostalCode = "6300", Description = "Sofa",
        EventId = eventId, PickupDate = pickupDate ?? new DateTime(2026, 11, 15)
    };

    private static ProcessedImage Processed(byte marker) =>
        new(new[] { marker, marker, marker }, new[] { marker });

    [Fact]
    public async Task CreateRequestAsync_StoresImageAndThumbnail_AsJpeg()
    {
        using (var ctx = _db.CreateContext())
            await CreateService(ctx).CreateRequestAsync(NewRequest(), new() { (Processed(7), "sofa.png") });

        using var check = _db.CreateContext();
        var image = await check.FurniturePickupImages.Include(i => i.Data).SingleAsync();
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal("sofa.jpg", image.FileName);
        Assert.Equal(new byte[] { 7, 7, 7 }, image.Data.ImageData);
        Assert.Equal(new byte[] { 7 }, image.Data.ThumbnailData);
    }

    [Fact]
    public async Task GetAllRequestsAsync_LoadsImageMetadataWithoutBytes()
    {
        using (var ctx = _db.CreateContext())
            await CreateService(ctx).CreateRequestAsync(NewRequest(), new() { (Processed(1), "a.jpg"), (Processed(2), "b.jpg") });

        using var read = _db.CreateContext();
        var requests = await CreateService(read).GetAllRequestsAsync();

        var images = Assert.Single(requests).Images;
        Assert.Equal(2, images.Count);
        Assert.All(images, i => Assert.Null(i.Data));
    }

    [Fact]
    public async Task GetImageContentAsync_ReturnsThumbnailOrFull_AndFallsBackForLegacyImages()
    {
        int newId, legacyId;
        using (var ctx = _db.CreateContext())
        {
            var request = NewRequest();
            request.Images.Add(new FurniturePickupImage
            {
                ContentType = "image/png", FileName = "alt.png",
                Data = new FurniturePickupImageData { ImageData = new byte[] { 9, 9 } }
            });
            await CreateService(ctx).CreateRequestAsync(request, new() { (Processed(5), "neu.jpg") });
            newId    = request.Images.Single(i => i.FileName == "neu.jpg").Id;
            legacyId = request.Images.Single(i => i.FileName == "alt.png").Id;
        }

        using var read = _db.CreateContext();
        var service = CreateService(read);

        Assert.Equal(new byte[] { 5 }, (await service.GetImageContentAsync(newId, thumbnail: true))!.Value.Data);
        Assert.Equal(new byte[] { 5, 5, 5 }, (await service.GetImageContentAsync(newId, thumbnail: false))!.Value.Data);

        var legacyThumb = (await service.GetImageContentAsync(legacyId, thumbnail: true))!.Value;
        Assert.Equal(new byte[] { 9, 9 }, legacyThumb.Data);
        Assert.Equal("image/png", legacyThumb.ContentType);

        Assert.Null(await service.GetImageContentAsync(9999, thumbnail: false));
    }

    [Fact]
    public async Task DeleteExpiredImagesAsync_DeletesOnlyPhotosOlderThanThreeMonthsAfterEvent()
    {
        using (var ctx = _db.CreateContext())
        {
            var oldEvent = new Event { Name = "Flomi alt", Date = new DateTime(2026, 3, 20) };
            var newEvent = new Event { Name = "Flomi neu", Date = new DateTime(2026, 9, 20) };
            ctx.Events.AddRange(oldEvent, newEvent);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            await service.CreateRequestAsync(NewRequest(oldEvent.Id), new() { (Processed(1), "alt.jpg") });
            await service.CreateRequestAsync(NewRequest(newEvent.Id), new() { (Processed(2), "neu.jpg") });
            // Ohne Event zählt das Abholdatum
            await service.CreateRequestAsync(NewRequest(pickupDate: new DateTime(2026, 1, 10)), new() { (Processed(3), "ohne-event.jpg") });
        }

        int deleted;
        using (var ctx = _db.CreateContext())
            deleted = await CreateService(ctx).DeleteExpiredImagesAsync(new DateTime(2026, 10, 23));

        using var check = _db.CreateContext();
        Assert.Equal(2, deleted);
        var remaining = await check.FurniturePickupImages.Select(i => i.FileName).ToListAsync();
        Assert.Equal(new[] { "neu.jpg" }, remaining);
        Assert.Equal(3, await check.FurniturePickupRequests.CountAsync()); // Anfragen selbst bleiben
    }

    [Fact]
    public async Task ReprocessLegacyImagesAsync_ShrinksOldImagesAndMarksUnreadableOnes()
    {
        using (var ctx = _db.CreateContext())
        {

            var request = NewRequest();
            request.Images.Add(new FurniturePickupImage
            {
                ContentType = "image/png", FileName = "gross.png",
                Data = new FurniturePickupImageData { ImageData = TestImages.Png(3200, 2400, SkiaSharp.SKColors.Olive) }
            });
            request.Images.Add(new FurniturePickupImage
            {
                ContentType = "image/jpeg", FileName = "kaputt.jpg",
                Data = new FurniturePickupImageData { ImageData = new byte[] { 0xFF, 0xD8, 0xFF, 0x00 } }
            });
            await CreateService(ctx).CreateRequestAsync(request, new());
        }

        using (var ctx = _db.CreateContext())
            Assert.Equal(2, await CreateService(ctx).ReprocessLegacyImagesAsync(10));

        using var check = _db.CreateContext();
        var images = await check.FurniturePickupImages.Include(i => i.Data).OrderBy(i => i.Id).ToListAsync();

        var shrunk = images[0];
        Assert.Equal("gross.jpg", shrunk.FileName);
        Assert.Equal("image/jpeg", shrunk.ContentType);
        Assert.Equal((1600, 1200), TestImages.Size(shrunk.Data.ImageData));
        Assert.NotEmpty(shrunk.Data.ThumbnailData!);

        var broken = images[1];
        Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }, broken.Data.ImageData);
        Assert.Empty(broken.Data.ThumbnailData!);

        // Zweiter Lauf findet nichts mehr
        using var again = _db.CreateContext();
        Assert.Equal(0, await CreateService(again).ReprocessLegacyImagesAsync(10));
    }
}
