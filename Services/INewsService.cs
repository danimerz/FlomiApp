using FlomiApp.Data.Models;
using FlomiApp.Helpers;

namespace FlomiApp.Services;

public interface INewsService
{
    /// <summary>Höchstzahl Bilder pro Meldung.</summary>
    const int MaxImages = 6;

    /// <summary>Alle Meldungen inkl. Bild-Metadaten (ohne Bilddaten).</summary>
    Task<List<NewsItem>> GetAllAsync();
    Task<List<NewsItem>> GetPublishedAsync();
    Task<NewsItem?>      GetLatestPublishedAsync();

    /// <param name="newImages">Bereits über <see cref="PickupImageProcessor"/> aufbereitete Bilder.</param>
    Task CreateAsync(NewsItem item, IReadOnlyList<(ProcessedImage Image, string FileName)>? newImages = null);

    /// <param name="removedImageIds">Bilder der Meldung, die entfernt werden.</param>
    Task UpdateAsync(NewsItem item,
                     IReadOnlyList<(ProcessedImage Image, string FileName)>? newImages = null,
                     IReadOnlyCollection<int>? removedImageIds = null);

    Task DeleteAsync(int id);
    Task TogglePublishedAsync(int id);
    Task TogglePinnedAsync(int id);

    /// <summary>
    /// Bilddaten für <c>/news-images/{id}</c>. Bilder von Entwürfen nur mit <paramref name="includeUnpublished"/>.
    /// </summary>
    Task<(byte[] Data, string ContentType)?> GetImageContentAsync(int imageId, bool thumbnail, bool includeUnpublished);
}
