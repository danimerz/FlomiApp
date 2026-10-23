using FlomiApp.Helpers;
using SkiaSharp;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class PickupImageProcessorTests
{
    [Fact]
    public void Process_ShrinksLargePhotoAndCreatesThumbnail()
    {
        var result = PickupImageProcessor.Process(TestImages.Jpeg(4000, 3000));

        Assert.NotNull(result);
        Assert.Equal((1600, 1200), TestImages.Size(result!.Data));
        Assert.Equal((400, 300), TestImages.Size(result.Thumbnail));
        Assert.Equal("image/jpeg", ImageSignatureHelper.DetectContentType(result.Data));
    }

    [Fact]
    public void Process_RemovesGpsAndOtherExifData()
    {
        var input = TestImages.WithExif(TestImages.Jpeg(800, 600), orientation: 1);
        Assert.True(TestImages.ContainsExif(input));

        var result = PickupImageProcessor.Process(input);

        Assert.NotNull(result);
        Assert.False(TestImages.ContainsExif(result!.Data));
        Assert.False(TestImages.ContainsExif(result.Thumbnail));
    }

    [Fact]
    public void Process_AppliesExifOrientation()
    {
        // Orientierung 6 = Kamera um 90° gedreht: 300×100 gespeichert, 100×300 gemeint
        var input = TestImages.WithExif(TestImages.Jpeg(300, 100), orientation: 6);
        using (var codec = SKCodec.Create(SKData.CreateCopy(input)))
            Assert.Equal(SKEncodedOrigin.RightTop, codec.EncodedOrigin);

        var result = PickupImageProcessor.Process(input);

        Assert.Equal((100, 300), TestImages.Size(result!.Data));
    }

    [Fact]
    public void Process_RotatesPixelsCorrectly()
    {
        // Linke Hälfte rot, rechte blau; nach 90° im Uhrzeigersinn ist oben rot, unten blau
        using var bitmap = new SKBitmap(200, 100);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Blue);
            canvas.DrawRect(0, 0, 100, 100, new SKPaint { Color = SKColors.Red });
        }
        using var encoded = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Jpeg, 95);
        var input = TestImages.WithExif(encoded.ToArray(), orientation: 6);

        using var output = SKBitmap.Decode(PickupImageProcessor.Process(input)!.Data);

        Assert.Equal((100, 200), (output.Width, output.Height));
        var top    = output.GetPixel(50, 20);
        var bottom = output.GetPixel(50, 180);
        Assert.True(top.Red > 200 && top.Blue < 60, $"oben: {top}");
        Assert.True(bottom.Blue > 200 && bottom.Red < 60, $"unten: {bottom}");
    }

    [Fact]
    public void Process_DoesNotUpscaleSmallImages()
    {
        var result = PickupImageProcessor.Process(TestImages.Jpeg(200, 100));

        Assert.NotNull(result);
        Assert.Equal((200, 100), TestImages.Size(result!.Data));
        Assert.Equal((200, 100), TestImages.Size(result.Thumbnail));
    }

    [Fact]
    public void Process_FlattensTransparentPngOntoWhite()
    {
        var result = PickupImageProcessor.Process(TestImages.Png(50, 50, SKColors.Transparent));

        Assert.NotNull(result);
        using var decoded = SKBitmap.Decode(result!.Data);
        var pixel = decoded.GetPixel(25, 25);
        Assert.True(pixel.Red > 245 && pixel.Green > 245 && pixel.Blue > 245, $"Pixel ist {pixel}, erwartet weiss");
    }

    [Fact]
    public void Process_ReturnsNullForNonImage()
    {
        // JPEG-Signatur, aber kaputter Inhalt
        var data = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x00, 0x01 };
        Assert.Null(PickupImageProcessor.Process(data));
    }
}
