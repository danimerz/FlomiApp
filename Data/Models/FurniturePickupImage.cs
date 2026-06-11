// Data/Models/FurniturePickupImage.cs
namespace FlomiApp.Data.Models;

public class FurniturePickupImage
{
    public int Id { get; set; }

    public int FurniturePickupRequestId { get; set; }
    public FurniturePickupRequest FurniturePickupRequest { get; set; } = null!;

    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public string FileName { get; set; } = string.Empty;
    public string? Caption { get; set; }
}