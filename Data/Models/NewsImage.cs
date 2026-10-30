namespace FlomiApp.Data.Models;

/// <summary>
/// Bild zu einer News-Meldung. Bilddaten wie bei den Möbelfotos per Table Splitting
/// in <see cref="NewsImageData"/>, damit Listen nur die Metadaten laden.
/// </summary>
public class NewsImage
{
    public int Id { get; set; }

    public int      NewsItemId { get; set; }
    public NewsItem NewsItem   { get; set; } = null!;

    public string ContentType { get; set; } = "image/jpeg";
    public string FileName    { get; set; } = string.Empty;

    /// <summary>Reihenfolge in der Meldung; das erste Bild erscheint auch auf der Startseite.</summary>
    public int SortOrder { get; set; }

    /// <summary>Bilddaten – nur bei Bedarf laden.</summary>
    public NewsImageData Data { get; set; } = null!;
}

/// <summary>Bilddaten zu <see cref="NewsImage"/> (gleiche Tabelle, gleiche Id).</summary>
public class NewsImageData
{
    public int Id { get; set; }

    public byte[] ImageData     { get; set; } = Array.Empty<byte>();
    public byte[] ThumbnailData { get; set; } = Array.Empty<byte>();
}
