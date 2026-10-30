using FlomiApp.Data.Models;

namespace FlomiApp.Services;

/// <summary>
/// Verwaltung der Passkeys (Liste, Umbenennen, Löschen). Erstellen und Anmelden laufen über
/// den SignInManager (braucht den HttpContext) in <c>Endpoints/PasskeyEndpoints.cs</c> bzw. der Login-Seite.
/// </summary>
public interface IPasskeyService
{
    /// <summary>Passkeys eines Benutzers, älteste zuerst (ohne Schlüsseldaten zu verwenden).</summary>
    Task<List<UserPasskey>> GetForUserAsync(string userId);

    Task<int> CountForUserAsync(string userId);

    /// <summary>Anzahl Passkeys je Benutzer-Id (für die Benutzerverwaltung).</summary>
    Task<Dictionary<string, int>> CountByUserAsync();

    /// <summary>Benennt einen eigenen Passkey um. False, wenn er nicht dem Benutzer gehört.</summary>
    Task<bool> RenameAsync(string userId, int passkeyId, string? name);

    /// <summary>Löscht einen eigenen Passkey und liefert ihn zurück (für Meldung/Audit), sonst null.</summary>
    Task<UserPasskey?> DeleteAsync(string userId, int passkeyId);

    /// <summary>Löscht alle Passkeys eines Benutzers (Admin, z.B. bei verlorenem Handy). Liefert die Anzahl.</summary>
    Task<int> DeleteAllAsync(string userId);

    /// <summary>Merkt sich den Zeitpunkt der letzten Anmeldung mit diesem Passkey.</summary>
    Task MarkUsedAsync(byte[] credentialId);
}
