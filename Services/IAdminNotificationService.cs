namespace FlomiApp.Services;

/// <summary>
/// Mails an Admins (Rolle «Admin») bei neuen Konten und bei Anmeldungen/Stornierungen.
/// Wer was bekommt, stellt jede Person unter Einstellungen → Profil ein
/// (<c>NotifyNewUsers</c>, <c>NotifyRegistrations</c>). Fehler werden nur geloggt –
/// eine Benachrichtigung darf nie eine Anmeldung verhindern.
/// </summary>
public interface IAdminNotificationService
{
    /// <param name="method">Wie das Konto erstellt wurde, z.B. «E-Mail und Passwort» oder «Google».</param>
    Task NotifyNewUserAsync(string userId, string method);

    Task NotifyRegistrationAsync(int appointmentId, bool cancelled = false);
}
