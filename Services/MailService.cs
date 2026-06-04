using FlomiApp.Data.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using QRCoder;

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

            // SSL-Zertifikat wird standardmässig validiert (sicherer Standard)

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
        string eventName, DateTime date, string timeSlot,
        string? comment = null, int? appointmentId = null, string? forPerson = null)
    {
        var subject = $"✅ Anmeldebestätigung – {areaName} · {eventName}";
        var commentRow = string.IsNullOrEmpty(comment) ? "" :
            $"<tr><td style='padding:6px 12px;color:#64748b;'>Dein Kommentar:</td><td style='padding:6px 12px;font-style:italic;'>{comment}</td></tr>";
        var forPersonRow = string.IsNullOrEmpty(forPerson) ? "" :
            $"<tr style='background:#fef9c3;'><td style='padding:8px 12px;color:#854d0e;font-weight:700;'>Angemeldet für:</td><td style='padding:8px 12px;font-weight:800;color:#854d0e;'>{forPerson}</td></tr>";

        var qrSection = "";
        if (appointmentId.HasValue)
        {
            var qrData   = $"flomi-apt:{appointmentId.Value}";
            var qrGen    = new QRCodeGenerator();
            var qrCodeData = qrGen.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            var qrCode   = new PngByteQRCode(qrCodeData);
            var qrPng    = qrCode.GetGraphic(6);
            var qrBase64 = Convert.ToBase64String(qrPng);
            qrSection = $"""
                <div style="margin-top:20px;text-align:center;padding:20px;background:#fff;border:1px solid #e2e8f0;border-radius:12px;">
                  <p style="margin:0 0 12px;font-weight:800;color:#1d4ed8;font-size:.95rem;text-transform:uppercase;letter-spacing:.04em;">📷 Dein Check-in QR-Code</p>
                  <img src="data:image/png;base64,{qrBase64}" style="width:180px;height:180px;border-radius:8px;" alt="QR-Code" />
                  <p style="margin:10px 0 0;font-size:.8rem;color:#64748b;">Zeige diesen Code beim Check-in vor. Er ist auch im Dashboard verfügbar.</p>
                </div>
                """;
        }

        var body = $"""
            <div style="font-family:sans-serif;max-width:540px;margin:0 auto;">
              <div style="background:linear-gradient(135deg,#1d4ed8,#2563eb);border-radius:16px 16px 0 0;padding:28px 32px;">
                <h1 style="margin:0;color:#fff;font-size:1.5rem;">Anmeldebestätigung 🎉</h1>
              </div>
              <div style="background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 16px 16px;padding:28px 32px;">
                <p style="color:#374151;margin-top:0;">Hallo <strong>{toName}</strong>,<br>deine Anmeldung war erfolgreich!</p>
                <table cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;margin:16px 0;background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;">
                  <tr style="background:#dbeafe;">
                    <td colspan="2" style="padding:10px 12px;font-weight:800;color:#1d4ed8;font-size:.85rem;letter-spacing:.04em;text-transform:uppercase;">Dein Einsatz</td>
                  </tr>
                  {forPersonRow}
                  <tr><td style="padding:6px 12px;color:#64748b;width:40%">Event:</td><td style="padding:6px 12px;font-weight:700;">{eventName}</td></tr>
                  <tr style="background:#f8fafc;"><td style="padding:6px 12px;color:#64748b;">Bereich:</td><td style="padding:6px 12px;font-weight:700;">{areaName}</td></tr>
                  <tr><td style="padding:6px 12px;color:#64748b;">Datum:</td><td style="padding:6px 12px;font-weight:700;">{date:dddd, dd. MMMM yyyy}</td></tr>
                  <tr style="background:#f8fafc;"><td style="padding:6px 12px;color:#64748b;">Zeit:</td><td style="padding:6px 12px;font-weight:700;">{timeSlot}</td></tr>
                  {commentRow}
                </table>
                {qrSection}
                <p style="color:#64748b;font-size:.9rem;margin-top:16px;">Du kannst deine Anmeldungen jederzeit im <a href="/user/dashboard" style="color:#2563eb;">Dashboard</a> einsehen oder stornieren.</p>
                <p style="color:#374151;font-weight:700;margin-bottom:0;">Wir freuen uns auf dich! 🙌</p>
              </div>
            </div>
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