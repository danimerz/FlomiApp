using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class NewsService : INewsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public NewsService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Bild-Metadaten in Anzeigereihenfolge, ohne Bilddaten
    private static IQueryable<NewsItem> WithImages(ApplicationDbContext db) =>
        db.NewsItems.AsNoTracking().Include(n => n.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id));

    public async Task<List<NewsItem>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await WithImages(db)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<NewsItem>> GetPublishedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await WithImages(db)
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<NewsItem?> GetLatestPublishedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await WithImages(db)
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(NewsItem item, IReadOnlyList<(ProcessedImage Image, string FileName)>? newImages = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        item.CreatedAt = DateTime.Now;
        item.Images    = new();
        AddImages(item, newImages, startOrder: 0);
        db.NewsItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(NewsItem item,
                                  IReadOnlyList<(ProcessedImage Image, string FileName)>? newImages = null,
                                  IReadOnlyCollection<int>? removedImageIds = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.NewsItems
            .Include(n => n.Images)
            .FirstOrDefaultAsync(n => n.Id == item.Id);
        if (existing is null) return;

        existing.Title       = item.Title;
        existing.Content     = item.Content;
        existing.IsPublished = item.IsPublished;
        existing.IsPinned    = item.IsPinned;

        if (removedImageIds is { Count: > 0 })
        {
            // Bilddaten mitladen: bei Table Splitting müssen beide Teile gelöscht werden
            var removed = existing.Images.Where(i => removedImageIds.Contains(i.Id)).ToList();
            foreach (var image in removed)
            {
                await db.Entry(image).Reference(i => i.Data).LoadAsync();
                existing.Images.Remove(image);
                db.NewsImages.Remove(image);
            }
        }

        var nextOrder = existing.Images.Count == 0 ? 0 : existing.Images.Max(i => i.SortOrder) + 1;
        AddImages(existing, newImages, nextOrder);

        await db.SaveChangesAsync();
    }

    private static void AddImages(NewsItem item, IReadOnlyList<(ProcessedImage Image, string FileName)>? newImages, int startOrder)
    {
        if (newImages is null) return;

        var free = INewsService.MaxImages - item.Images.Count;
        foreach (var (image, fileName) in newImages.Take(Math.Max(free, 0)))
        {
            item.Images.Add(new NewsImage
            {
                ContentType = ProcessedImage.ContentType,
                FileName    = Path.ChangeExtension(Path.GetFileName(fileName), ".jpg"),
                SortOrder   = startOrder++,
                Data        = new NewsImageData { ImageData = image.Data, ThumbnailData = image.Thumbnail },
            });
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.NewsItems
            .Include(n => n.Images).ThenInclude(i => i.Data)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (item is null) return;
        db.NewsItems.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task TogglePublishedAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.NewsItems.FindAsync(id);
        if (item is null) return;
        item.IsPublished = !item.IsPublished;
        await db.SaveChangesAsync();
    }

    public async Task TogglePinnedAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.NewsItems.FindAsync(id);
        if (item is null) return;
        item.IsPinned = !item.IsPinned;
        await db.SaveChangesAsync();
    }

    public async Task<(byte[] Data, string ContentType)?> GetImageContentAsync(int imageId, bool thumbnail, bool includeUnpublished)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var images = db.NewsImages.AsNoTracking()
            .Where(i => i.Id == imageId && (includeUnpublished || i.NewsItem.IsPublished));

        var content = thumbnail
            ? await images.Select(i => new { Data = i.Data.ThumbnailData, i.ContentType }).FirstOrDefaultAsync()
            : await images.Select(i => new { Data = i.Data.ImageData,     i.ContentType }).FirstOrDefaultAsync();

        return content == null ? null : (content.Data, content.ContentType);
    }
}
