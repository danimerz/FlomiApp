// Data/Models/FurniturePickupImage.cs
namespace FlomiApp.Data.Models;

/// <summary>
/// Metadaten eines Fotos. Die Bilddaten liegen in derselben Tabelle, sind aber als
/// eigene Entität <see cref="FurniturePickupImageData"/> abgetrennt (Table Splitting),
/// damit Listen mit <c>Include(r => r.Images)</c> keine Megabytes mitladen.
/// </summary>
public class FurniturePickupImage
{
    public int Id { get; set; }

    public int FurniturePickupRequestId { get; set; }
    public FurniturePickupRequest FurniturePickupRequest { get; set; } = null!;

    public string ContentType { get; set; } = "image/jpeg";
    public string FileName { get; set; } = string.Empty;
    public string? Caption { get; set; }

    /// <summary>Bilddaten – nur bei Bedarf laden (<c>Include(i => i.Data)</c>).</summary>
    public FurniturePickupImageData Data { get; set; } = null!;
}

/// <summary>Bilddaten zu <see cref="FurniturePickupImage"/> (gleiche Tabelle, gleiche Id).</summary>
public class FurniturePickupImageData
{
    public int Id { get; set; }

    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>Vorschaubild (JPEG, max. 400 px). Null bei noch nicht nachbearbeiteten Altbildern.</summary>
    public byte[]? ThumbnailData { get; set; }
}
