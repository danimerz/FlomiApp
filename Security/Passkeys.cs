using System.Security.Claims;

namespace FlomiApp.Security;

/// <summary>
/// Anmeldung mit Passkey: Konstanten und Regeln, die Login, Policy und Einstellungen teilen.
/// </summary>
public static class Passkeys
{
    /// <summary>
    /// Wert des <c>amr</c>-Claims (Anmeldemethode) nach einer Passkey-Anmeldung.
    /// Identity setzt selbst «pwd» (Passwort) bzw. «mfa» (Passwort + Code).
    /// </summary>
    public const string AuthenticationMethod = "passkey";

    public const string AmrClaimType = "amr";

    /// <summary>Höchstzahl Passkeys pro Konto (mehrere Geräte, Ersatzschlüssel).</summary>
    public const int MaxPerUser = 10;

    public const int MaxNameLength = 100;

    /// <summary>Wurde diese Sitzung mit einem Passkey eröffnet?</summary>
    public static bool IsPasskeySession(ClaimsPrincipal user) =>
        user.HasClaim(AmrClaimType, AuthenticationMethod);

    /// <summary>
    /// Regel der Policy <see cref="Roles.RequireTwoFactorForAdmins"/>: Admins brauchen
    /// eingerichtete 2FA (Authenticator-App) – oder sie haben sich mit einem Passkey angemeldet.
    /// Ein Passkey gilt als zwei Faktoren (Gerät + Face ID/Fingerabdruck/PIN).
    /// </summary>
    public static bool MeetsAdminTwoFactorRequirement(ClaimsPrincipal user)
    {
        var isAdmin = Roles.AllAdminRoles.Any(user.IsInRole);
        return !isAdmin || user.HasClaim("tfa_enrolled", "true") || IsPasskeySession(user);
    }

    /// <summary>Anzeigename bereinigen; leer → «Passkey».</summary>
    public static string NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0) return "Passkey";
        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }
}
