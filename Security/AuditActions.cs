namespace FlomiApp.Security;

public static class AuditActions
{
    public const string UserDeleted        = "Benutzer gelöscht";
    public const string UserLocked         = "Benutzer gesperrt";
    public const string UserUnlocked       = "Benutzer entsperrt";
    public const string RoleChanged        = "Rolle geändert";
    public const string PasswordReset      = "Passwort gesetzt";
    public const string PasswordResetMailSent = "PW-Reset-Mail gesendet";
    public const string TwoFactorReset     = "2FA zurückgesetzt";
    public const string AppointmentCancelled = "Anmeldung storniert (Admin)";
}
