namespace FlomiApp.Helpers;

/// <summary>
/// Erkennt den tatsächlichen Bildtyp anhand der Datei-Signatur (Magic Bytes),
/// statt dem vom Browser gemeldeten (und damit manipulierbaren) Content-Type
/// zu vertrauen. Nur die vier hier erkannten Formate gelten als zulässig.
/// </summary>
public static class ImageSignatureHelper
{
    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif" };

    /// <summary>
    /// Liefert den anhand der Datei-Signatur erkannten Content-Type, oder null
    /// wenn die Bytes zu keinem der zulässigen Bildformate passen.
    /// </summary>
    public static string? DetectContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        if (data.Length >= 8 &&
            data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return "image/png";

        if (data.Length >= 6 &&
            data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38 &&
            (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61)
            return "image/gif";

        if (data.Length >= 12 &&
            data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && // "RIFF"
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) // "WEBP"
            return "image/webp";

        return null;
    }
}
