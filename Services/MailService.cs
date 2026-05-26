using FlomiApp.Data.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FlomiApp.Services;

public class MailService : IMailService
{
    private readonly SmtpSettings _smtp;
    private readonly ILogger<MailService> _logger;

    public MailService(IOptions<SmtpSettings> smtp, ILogger<MailService> logger)
    {
        _smtp   = smtp.Value;
        _logger = logger;
    }

    // ─── Generisch ────────────────────────────────────────────────────────────
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body    = new TextPart("html") { Text = WrapInLayout(htmlBody) };

            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("✅ Mail gesendet an {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Fehler beim Senden der Mail an {Email}", toEmail);
            throw;
        }
    }

    // ─── Benutzer-Mails ───────────────────────────────────────────────────────
    public async Task SendRegistrationConfirmationAsync(
        string toEmail, string toName, string areaName,
        string eventName, DateTime date, string timeSlot)
    {
        var subject = $"Anmeldebestätigung – {areaName}";
        var body = $"""
            <h2>Hallo {toName}! 🎉</h2>
            <p>Deine Anmeldung war erfolgreich.</p>
            <table cellpadding="8" style="border-collapse:collapse;">
                <tr><td><strong>Event:</strong></td><td>{eventName}</td></tr>
                <tr><td><strong>Bereich:</strong></td><td>{areaName}</td></tr>
                <tr><td><strong>Datum:</strong></td><td>{date:dd.MM.yyyy}</td></tr>
                <tr><td><strong>Zeit:</strong></td><td>{timeSlot}</td></tr>
            </table>
            <p>Wir freuen uns auf dich!</p>
            """;

        await SendAsync(toEmail, toName, subject, body);
    }

    public async Task SendCancellationConfirmationAsync(
        string toEmail, string toName, string areaName,
        string eventName, DateTime date, string timeSlot)
    {
        var subject = $"Stornierung – {areaName}";
        var body = $"""
            <h2>Hallo {toName}</h2>
            <p>Deine Anmeldung wurde erfolgreich storniert.</p>
            <table cellpadding="8" style="border-collapse:collapse;">
                <tr><td><strong>Event:</strong></td><td>{eventName}</td></tr>
                <tr><td><strong>Bereich:</strong></td><td>{areaName}</td></tr>
                <tr><td><strong>Datum:</strong></td><td>{date:dd.MM.yyyy}</td></tr>
                <tr><td><strong>Zeit:</strong></td><td>{timeSlot}</td></tr>
            </table>
            <p>Du kannst dich jederzeit neu anmelden.</p>
            """;

        await SendAsync(toEmail, toName, subject, body);
    }

    public async Task SendAppointmentSummaryAsync(
        string toEmail, string toName,
        List<AppointmentSummaryItem> appointments)
    {
        var subject = "Deine Anmeldungen – Übersicht";

        var rows = string.Join("\n", appointments.Select(a => $"""
            <tr>
                <td>{a.EventName}</td>
                <td>{a.AreaName}</td>
                <td>{a.Date:dd.MM.yyyy}</td>
                <td>{a.TimeSlot}</td>
            </tr>
            """));

        var body = $"""
            <h2>Hallo {toName}!</h2>
            <p>Hier ist deine aktuelle Anmeldungsübersicht:</p>
            <table cellpadding="8" border="1" style="border-collapse:collapse;">
                <thead>
                    <tr>
                        <th>Event</th>
                        <th>Bereich</th>
                        <th>Datum</th>
                        <th>Zeit</th>
                    </tr>
                </thead>
                <tbody>
                    {rows}
                </tbody>
            </table>
            """;

        await SendAsync(toEmail, toName, subject, body);
    }

    // ─── Admin-Mails ──────────────────────────────────────────────────────────
    public async Task SendAdminNewRegistrationAsync(
        string areaName, string eventName,
        string userName, DateTime date, string timeSlot)
    {
        var subject = $"Neue Anmeldung – {userName}";
        var body = $"""
            <h2>Neue Anmeldung eingegangen</h2>
            <table cellpadding="8" style="border-collapse:collapse;">
                <tr><td><strong>Person:</strong></td><td>{userName}</td></tr>
                <tr><td><strong>Event:</strong></td><td>{eventName}</td></tr>
                <tr><td><strong>Bereich:</strong></td><td>{areaName}</td></tr>
                <tr><td><strong>Datum:</strong></td><td>{date:dd.MM.yyyy}</td></tr>
                <tr><td><strong>Zeit:</strong></td><td>{timeSlot}</td></tr>
            </table>
            """;

        await SendAsync(_smtp.FromAddress, "Admin", subject, body);
    }

    // ✅ NEU
    public async Task SendAdminCancellationAsync(
        string areaName, string eventName,
        string userName, DateTime date, string timeSlot)
    {
        var subject = $"Stornierung – {userName}";
        var body = $"""
            <h2>Anmeldung storniert</h2>
            <table cellpadding="8" style="border-collapse:collapse;">
                <tr><td><strong>Person:</strong></td><td>{userName}</td></tr>
                <tr><td><strong>Event:</strong></td><td>{eventName}</td></tr>
                <tr><td><strong>Bereich:</strong></td><td>{areaName}</td></tr>
                <tr><td><strong>Datum:</strong></td><td>{date:dd.MM.yyyy}</td></tr>
                <tr><td><strong>Zeit:</strong></td><td>{timeSlot}</td></tr>
            </table>
            """;

        await SendAsync(_smtp.FromAddress, "Admin", subject, body);
    }

    // ─── Event-Ankündigung ────────────────────────────────────────────────────
    public async Task SendEventAnnouncementAsync(
        List<string> toEmails, string eventName,
        DateTime eventDate, string description)
    {
        var subject = $"Neues Event – {eventName}";
        var body = $"""
            <h2>Neues Event: {eventName} 🎊</h2>
            <p><strong>Datum:</strong> {eventDate:dd.MM.yyyy}</p>
            <p>{description}</p>
            <p>Melde dich jetzt an!</p>
            """;

        foreach (var email in toEmails)
        {
            await SendAsync(email, "", subject, body);
        }
    }

    // ─── Passwort-Reset ───────────────────────────────────────────────────────
    public async Task SendPasswordResetAsync(
        string toEmail, string toName, string resetLink)
    {
        var subject = "Passwort zurücksetzen";
        var body = $"""
            <h2>Hallo {toName}</h2>
            <p>Du hast eine Passwort-Zurücksetzung angefordert.</p>
            <p>
                <a href="{resetLink}" 
                   style="background:#007bff;color:white;padding:10px 20px;
                          text-decoration:none;border-radius:5px;">
                    Passwort zurücksetzen
                </a>
            </p>
            <p style="color:gray;font-size:12px;">
                Falls du das nicht angefordert hast, ignoriere diese Mail.
                Der Link ist 24 Stunden gültig.
            </p>
            """;

        await SendAsync(toEmail, toName, subject, body);
    }

    // ─── Layout ───────────────────────────────────────────────────────────────
    private string WrapInLayout(string content) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:20px;">
            {content}
            <hr style="margin-top:40px;"/>
            <p style="color:gray;font-size:11px;">
                FlomiApp – automatische Nachricht, bitte nicht antworten.
            </p>
        </body>
        </html>
        """;
}