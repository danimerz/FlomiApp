using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using FlomiApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlomiApp.Tests.Services;

public class NewsServiceImageTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private NewsService CreateService() => new(_db.CreateContextFactory());

    private static (ProcessedImage, string) Image(byte marker, string name = "foto.png") =>
        (new ProcessedImage(new[] { marker, marker, marker }, new[] { marker }), name);

    private static NewsItem News(bool published = true) =>
        new() { Title = "Flomi 2027", Content = "Wir suchen Helfer.", IsPublished = published };

    [Fact]
    public async Task CreateAsync_StoresImagesInOrder_ListsWithoutData()
    {
        var service = CreateService();
        await service.CreateAsync(News(), new[] { Image(1, "a.png"), Image(2, "b.heic") });

        var item = Assert.Single(await service.GetPublishedAsync());
        Assert.Equal(new[] { "a.jpg", "b.jpg" }, item.Images.Select(i => i.FileName));
        Assert.All(item.Images, i => Assert.Null(i.Data));

        var thumb = await service.GetImageContentAsync(item.Images[1].Id, thumbnail: true, includeUnpublished: false);
        Assert.Equal(new byte[] { 2 }, thumb?.Data);
        Assert.Equal("image/jpeg", thumb?.ContentType);
    }

    [Fact]
    public async Task CreateAsync_LimitsNumberOfImages()
    {
        var service = CreateService();
        var images  = Enumerable.Range(1, INewsService.MaxImages + 2).Select(i => Image((byte)i)).ToList();

        await service.CreateAsync(News(), images);

        Assert.Equal(INewsService.MaxImages, (await service.GetAllAsync())[0].Images.Count);
    }

    [Fact]
    public async Task GetImageContentAsync_HidesImagesOfDrafts()
    {
        var service = CreateService();
        await service.CreateAsync(News(published: false), new[] { Image(5) });
        var imageId = (await service.GetAllAsync())[0].Images[0].Id;

        Assert.Null(await service.GetImageContentAsync(imageId, thumbnail: false, includeUnpublished: false));
        Assert.Equal(new byte[] { 5, 5, 5 },
            (await service.GetImageContentAsync(imageId, thumbnail: false, includeUnpublished: true))?.Data);
    }

    [Fact]
    public async Task UpdateAsync_RemovesAndAppendsImages()
    {
        var service = CreateService();
        await service.CreateAsync(News(), new[] { Image(1, "a.png"), Image(2, "b.png") });
        var item = (await service.GetAllAsync())[0];

        item.Title = "Neu";
        await service.UpdateAsync(item, new[] { Image(3, "c.png") }, new[] { item.Images[0].Id });

        var updated = (await service.GetAllAsync())[0];
        Assert.Equal("Neu", updated.Title);
        Assert.Equal(new[] { "b.jpg", "c.jpg" }, updated.Images.Select(i => i.FileName));

        using var check = _db.CreateContext();
        Assert.Equal(2, await check.NewsImages.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_DeletesImages()
    {
        var service = CreateService();
        await service.CreateAsync(News(), new[] { Image(1), Image(2) });

        await service.DeleteAsync((await service.GetAllAsync())[0].Id);

        using var check = _db.CreateContext();
        Assert.Equal(0, await check.NewsImages.CountAsync());
    }
}
