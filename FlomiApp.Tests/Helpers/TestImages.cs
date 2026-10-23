using SkiaSharp;

namespace FlomiApp.Tests.Helpers;

/// <summary>Erzeugt Testbilder (JPEG/PNG, optional mit EXIF) und liest deren Grösse aus.</summary>
public static class TestImages
{
    public static byte[] Jpeg(int width, int height, SKColor? color = null) =>
        Encode(width, height, color ?? new SKColor(139, 69, 19), SKEncodedImageFormat.Jpeg);

    public static byte[] Png(int width, int height, SKColor color) =>
        Encode(width, height, color, SKEncodedImageFormat.Png);

    public static (int Width, int Height) Size(byte[] data)
    {
        using var codec = SKCodec.Create(SKData.CreateCopy(data));
        return (codec.Info.Width, codec.Info.Height);
    }

    public static bool ContainsExif(byte[] data) =>
        data.AsSpan().IndexOf("Exif\0\0"u8) >= 0;

    /// <summary>
    /// Fügt einem JPEG einen EXIF-Block (APP1) mit Orientierung und GPS-Breitengrad-Referenz
    /// ein – so wie ihn ein Handy schreibt, nur minimal.
    /// </summary>
    public static byte[] WithExif(byte[] jpeg, ushort orientation)
    {
        var tiff = new List<byte>();
        void U16(int v) { tiff.Add((byte)(v >> 8)); tiff.Add((byte)v); }
        void U32(int v) { U16(v >> 16); U16(v & 0xFFFF); }

        const int ifd0Entries = 2;
        var gpsIfdOffset = 8 + 2 + 12 * ifd0Entries + 4;

        // TIFF-Header, Big Endian, IFD0 ab Offset 8
        tiff.AddRange("MM"u8.ToArray()); U16(0x2A); U32(8);

        U16(ifd0Entries);
        U16(0x0112); U16(3); U32(1); U16(orientation); U16(0); // Orientation (SHORT)
        U16(0x8825); U16(4); U32(1); U32(gpsIfdOffset);        // Zeiger auf GPS-IFD (LONG)
        U32(0);                                                // kein weiteres IFD

        U16(1);
        U16(0x0001); U16(2); U32(2); tiff.Add((byte)'N'); tiff.Add(0); U16(0); // GPSLatitudeRef "N"
        U32(0);

        var app1 = new List<byte> { 0xFF, 0xE1 };
        var length = 2 + 6 + tiff.Count;
        app1.Add((byte)(length >> 8)); app1.Add((byte)length);
        app1.AddRange("Exif\0\0"u8.ToArray());
        app1.AddRange(tiff);

        // Direkt nach SOI (FF D8) einfügen
        return jpeg.Take(2).Concat(app1).Concat(jpeg.Skip(2)).ToArray();
    }

    private static byte[] Encode(int width, int height, SKColor color, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data  = image.Encode(format, 90);
        return data.ToArray();
    }
}
