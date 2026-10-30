namespace FlomiApp.Data.Models;

/// <summary>
/// Passkey (WebAuthn-Anmeldedaten) eines Benutzers. Gespeichert wird nur der öffentliche
/// Schlüssel – das Geheimnis bleibt auf dem Gerät bzw. im Passwortmanager.
/// Eigene Tabelle, weil die Identity-Passkey-Tabelle erst mit EF Core 10 kommt
/// (siehe <see cref="FlomiApp.Security.ApplicationUserStore"/>).
/// </summary>
public class UserPasskey
{
    public int Id { get; set; }

    public string          UserId { get; set; } = string.Empty;
    public ApplicationUser User   { get; set; } = null!;

    /// <summary>Credential-ID vom Gerät (max. 1023 Bytes laut WebAuthn), eindeutig.</summary>
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey    { get; set; } = Array.Empty<byte>();

    /// <summary>Anzeigename, z.B. «iPhone» – vom Benutzer änderbar.</summary>
    public string? Name { get; set; }

    public DateTime  CreatedAt  { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public uint SignCount { get; set; }

    /// <summary>Übertragungswege (usb, nfc, ble, internal, hybrid) kommagetrennt.</summary>
    public string? Transports { get; set; }

    public bool IsUserVerified   { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp       { get; set; }

    public byte[] AttestationObject { get; set; } = Array.Empty<byte>();
    public byte[] ClientDataJson    { get; set; } = Array.Empty<byte>();
}
