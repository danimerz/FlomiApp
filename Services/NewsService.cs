using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class NewsService : INewsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public NewsService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<NewsItem>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NewsItems
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<NewsItem>> GetPublishedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NewsItems
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<NewsItem?> GetLatestPublishedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NewsItems
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(NewsItem item)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        item.CreatedAt = DateTime.Now;
        db.NewsItems.Add(item);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(NewsItem item)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.NewsItems.FindAsync(item.Id);
        if (existing is null) return;
        existing.Title       = item.Title;
        existing.Content     = item.Content;
        existing.IsPublished = item.IsPublished;
        existing.IsPinned    = item.IsPinned;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.NewsItems.FindAsync(id);
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
}
