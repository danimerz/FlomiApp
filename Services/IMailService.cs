using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IMailService
{
    // Benutzer-Mails
    Task SendRegistrationConfirmationAsync(string toEmail, string toName,
        string areaName, string eventName, DateTime date, string timeSlot,
        string? comment = null, int? appointmentId = null);

    Task SendCancellationConfirmationAsync(string toEmail, string toName,
        string areaName, string eventName, DateTime date, string timeSlot);

    Task SendAppointmentSummaryAsync(string toEmail, string toName,
        List<AppointmentSummaryItem> appointments);

    // Admin-Mails
    Task SendAdminNewRegistrationAsync(string areaName, string eventName,
        string userName, DateTime date, string timeSlot);

    // ✅ NEU
    Task SendAdminCancellationAsync(string areaName, string eventName,
        string userName, DateTime date, string timeSlot);

    // Event-Ankündigung
    Task SendEventAnnouncementAsync(List<string> toEmails,
        string eventName, DateTime eventDate, string description);

    // Passwort-Reset (Identity)
    Task SendPasswordResetAsync(string toEmail, string toName, string resetLink);

    // Generisch
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}