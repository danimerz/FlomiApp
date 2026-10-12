using FlomiApp.Helpers;
using Xunit;

namespace FlomiApp.Tests.Helpers;

public class ImageSignatureHelperTests
{
    [Fact]
    public void DetectContentType_RecognizesJpeg()
    {
        var data = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        Assert.Equal("image/jpeg", ImageSignatureHelper.DetectContentType(data));
    }

    [Fact]
    public void DetectContentType_RecognizesPng()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        Assert.Equal("image/png", ImageSignatureHelper.DetectContentType(data));
    }

    [Theory]
    [InlineData(0x37)] // GIF87a
    [InlineData(0x39)] // GIF89a
    public void DetectContentType_RecognizesBothGifVariants(byte versionByte)
    {
        var data = new byte[] { 0x47, 0x49, 0x46, 0x38, versionByte, 0x61, 0x00, 0x00 };
        Assert.Equal("image/gif", ImageSignatureHelper.DetectContentType(data));
    }

    [Fact]
    public void DetectContentType_RecognizesWebp()
    {
        var data = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
        Assert.Equal("image/webp", ImageSignatureHelper.DetectContentType(data));
    }

    [Fact]
    public void DetectContentType_RejectsRenamedPdf()
    {
        // Angriffsfall: ein PDF, das als .jpg hochgeladen wird — der Browser
        // meldet trotzdem "image/jpeg" als Content-Type.
        var data = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%fake");
        Assert.Null(ImageSignatureHelper.DetectContentType(data));
    }

    [Fact]
    public void DetectContentType_RejectsRiffContainerThatIsNotWebp()
    {
        // RIFF-Header allein reicht nicht — es könnte z.B. eine WAV-Datei sein.
        var data = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45 };
        Assert.Null(ImageSignatureHelper.DetectContentType(data));
    }

    [Fact]
    public void DetectContentType_ReturnsNull_ForEmptyData()
    {
        Assert.Null(ImageSignatureHelper.DetectContentType(Array.Empty<byte>()));
    }

    [Fact]
    public void DetectContentType_ReturnsNull_ForTruncatedHeader()
    {
        var data = new byte[] { 0x89, 0x50, 0x4E }; // zu kurz für den vollen PNG-Header
        Assert.Null(ImageSignatureHelper.DetectContentType(data));
    }
}
