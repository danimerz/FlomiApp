using SkiaSharp;

namespace FlomiApp.Helpers;

/// <summary>Verkleinertes Foto plus Vorschaubild, beide als JPEG.</summary>
public sealed record ProcessedImage(byte[] Data, byte[] Thumbnail)
{
    public const string ContentType = "image/jpeg";
}

/// <summary>
/// Bereitet hochgeladene Möbelfotos für die Speicherung auf: richtig drehen,
/// auf max. 1600 px verkleinern und als JPEG (Qualität 80) neu kodieren. Beim
/// Neukodieren fallen alle Metadaten weg – Handyfotos enthalten sonst die
/// GPS-Position, also meist die Wohnadresse. Dazu ein Vorschaubild mit max. 400 px.
/// </summary>
public static class PickupImageProcessor
{
    public const int MaxDimension       = 1600;
    public const int ThumbnailDimension = 400;
    public const int JpegQuality        = 80;

    // Schutz vor Dekompressionsbomben: ein 5-MB-PNG kann sonst Gigabytes belegen.
    private const long MaxPixels = 60_000_000;

    private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

    /// <summary>
    /// Liefert das aufbereitete Bild, oder null wenn die Bytes kein lesbares
    /// Bild sind (oder unplausibel gross).
    /// </summary>
    public static ProcessedImage? Process(byte[] data)
    {
        using var skData = SKData.CreateCopy(data);
        using var codec  = SKCodec.Create(skData);
        if (codec == null) return null;

        var (width, height) = (codec.Info.Width, codec.Info.Height);
        if (width <= 0 || height <= 0 || (long)width * height > MaxPixels) return null;

        using var decoded = Decode(codec);
        if (decoded == null) return null;

        using var full  = Render(decoded, codec.EncodedOrigin, MaxDimension);
        using var thumb = Render(full, SKEncodedOrigin.TopLeft, ThumbnailDimension);

        return new ProcessedImage(Encode(full), Encode(thumb));
    }

    /// <summary>
    /// Dekodiert nach sRGB. Grosse JPEGs lässt Skia schon beim Dekodieren
    /// verkleinern (1/2, 1/4, 1/8) – spart bei 48-MP-Handyfotos viel Speicher.
    /// </summary>
    private static SKBitmap? Decode(SKCodec codec)
    {
        var longest = Math.Max(codec.Info.Width, codec.Info.Height);
        var size    = codec.Info.Size;
        if (longest > MaxDimension)
        {
            var scaled = codec.GetScaledDimensions((float)MaxDimension / longest);
            // Nur übernehmen, wenn das Ergebnis nicht unter die Zielgrösse fällt
            if (Math.Max(scaled.Width, scaled.Height) >= MaxDimension) size = scaled;
        }

        var info   = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());
        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result is SKCodecResult.Success or SKCodecResult.IncompleteInput) return bitmap;

        bitmap.Dispose();
        return null;
    }

    /// <summary>
    /// Zeichnet das Bild gedreht gemäss EXIF-Orientierung, verkleinert auf
    /// <paramref name="maxDimension"/> (nie vergrössert) und auf weissem Grund –
    /// JPEG kennt keine Transparenz.
    /// </summary>
    private static SKBitmap Render(SKBitmap source, SKEncodedOrigin origin, int maxDimension)
    {
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                          or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var (orientedW, orientedH) = swap ? (source.Height, source.Width) : (source.Width, source.Height);

        var scale = Math.Min(1f, (float)maxDimension / Math.Max(orientedW, orientedH));
        var tw = Math.Max(1, (int)Math.Round(orientedW * scale));
        var th = Math.Max(1, (int)Math.Round(orientedH * scale));
        // Zielgrösse im Koordinatensystem des ungedrehten Quellbilds
        var (sw, sh) = swap ? (th, tw) : (tw, th);

        var target = new SKBitmap(new SKImageInfo(tw, th, SKColorType.Rgba8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb()));
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.White);
        canvas.SetMatrix(OrientationMatrix(origin, tw, th));

        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, SKRect.Create(sw, sh), Sampling);
        canvas.Flush();
        return target;
    }

    // x' = ScaleX·x + SkewX·y + TransX,  y' = SkewY·x + ScaleY·y + TransY
    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, float w, float h) => origin switch
    {
        SKEncodedOrigin.TopRight    => new SKMatrix(-1,  0, w,  0,  1, 0, 0, 0, 1), // horizontal gespiegelt
        SKEncodedOrigin.BottomRight => new SKMatrix(-1,  0, w,  0, -1, h, 0, 0, 1), // 180°
        SKEncodedOrigin.BottomLeft  => new SKMatrix( 1,  0, 0,  0, -1, h, 0, 0, 1), // vertikal gespiegelt
        SKEncodedOrigin.LeftTop     => new SKMatrix( 0,  1, 0,  1,  0, 0, 0, 0, 1), // transponiert
        SKEncodedOrigin.RightTop    => new SKMatrix( 0, -1, w,  1,  0, 0, 0, 0, 1), // 90° im Uhrzeigersinn
        SKEncodedOrigin.RightBottom => new SKMatrix( 0, -1, w, -1,  0, h, 0, 0, 1), // transversal
        SKEncodedOrigin.LeftBottom  => new SKMatrix( 0,  1, 0, -1,  0, h, 0, 0, 1), // 90° gegen Uhrzeigersinn
        _                           => SKMatrix.Identity
    };

    private static byte[] Encode(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return data.ToArray();
    }
}
