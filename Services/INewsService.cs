using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface INewsService
{
    Task<List<NewsItem>> GetAllAsync();
    Task<List<NewsItem>> GetPublishedAsync();
    Task<NewsItem?>      GetLatestPublishedAsync();
    Task                 CreateAsync(NewsItem item);
    Task                 UpdateAsync(NewsItem item);
    Task                 DeleteAsync(int id);
    Task                 TogglePublishedAsync(int id);
    Task                 TogglePinnedAsync(int id);
}
