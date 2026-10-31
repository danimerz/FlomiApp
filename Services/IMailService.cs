using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IMailService
{
    // Benutzer-Mails
    Task SendRegistrationConfirmationAsync(string toEmail, string toName,
        string areaName, string eventName, DateTime? date, string timeSlot,
        string? comment = null, int? appointmentId = null, string? forPerson = null);

    Task SendCancellationConfirmationAsync(string toEmail, string toName,
        string areaName, string eventName, DateTime? date, string timeSlot);

    Task SendAppointmentSummaryAsync(string toEmail, string toName,
        List<AppointmentSummaryItem> appointments);

    // Admin-Mails (Empfänger bestimmt IAdminNotificationService)
    Task SendAdminNewUserAsync(string toEmail, AdminNewUserMail mail);

    Task SendAdminRegistrationAsync(string toEmail, AdminRegistrationMail mail);

    // Event-Ankündigung
    Task SendEventAnnouncementAsync(List<string> toEmails,
        string eventName, DateTime eventDate, string description);

    // Passwort-Reset (Identity)
    Task SendPasswordResetAsync(string toEmail, string toName, string resetLink);

    // E-Mail-Bestätigung bei Registrierung (Identity)
    Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink);

    // Generisch
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}