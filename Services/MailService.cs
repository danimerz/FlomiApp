using FlomiApp.Data.Models;
using FlomiApp.Helpers;
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
        string eventName, DateTime? date, string timeSlot,
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
                  <tr><td style="padding:6px 12px;color:#64748b;">Datum:</td><td style="padding:6px 12px;font-weight:700;">{AreaSchedule.FormatDate(date, "dddd, dd. MMMM yyyy")}</td></tr>
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
        string eventName, DateTime? date, string timeSlot, string? forPerson = null)
    {
        var subject = $"Stornierung – {areaName}";
        var body = $"""
            <h2>Hallo {toName}</h2>
            <p>Deine Anmeldung wurde erfolgreich storniert.</p>
            <table cellpadding="8" style="border-collapse:collapse;">
                {(string.IsNullOrEmpty(forPerson) ? "" : $"<tr><td><strong>Person:</strong></td><td>{System.Net.WebUtility.HtmlEncode(forPerson)}</td></tr>")}
                <tr><td><strong>Event:</strong></td><td>{eventName}</td></tr>
                <tr><td><strong>Bereich:</strong></td><td>{areaName}</td></tr>
                <tr><td><strong>Datum:</strong></td><td>{AreaSchedule.FormatDate(date, "dd.MM.yyyy")}</td></tr>
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
        var subject = "📋 Deine Einsatz-Übersicht";

        var grouped = appointments
            .GroupBy(a => a.EventName)
            .OrderBy(g => g.Key);

        var sections = new System.Text.StringBuilder();
        foreach (var group in grouped)
        {
            var rows = new System.Text.StringBuilder();
            int i = 0;
            foreach (var a in group.OrderBy(x => x.Date ?? DateTime.MaxValue))
            {
                var bg         = i++ % 2 == 0 ? "" : "background:#f8fafc;";
                var personCell = string.IsNullOrEmpty(a.ForPerson)
                    ? ""
                    : $"<td style='padding:6px 12px;color:#374151;'>{a.ForPerson}</td>";
                rows.Append($"""
                    <tr style="{bg}">
                      <td style='padding:6px 12px;font-weight:600;color:#1e293b;'>{a.AreaName}</td>
                      <td style='padding:6px 12px;color:#374151;'>{AreaSchedule.FormatDate(a.Date, "dd.MM.yyyy")}</td>
                      <td style='padding:6px 12px;color:#374151;'>{a.TimeSlot}</td>
                      <td style='padding:6px 12px;color:#374151;'>{a.Location ?? "–"}</td>
                      {personCell}
                    </tr>
                    """);
            }

            var hasPersonCol = group.Any(a => !string.IsNullOrEmpty(a.ForPerson));
            var personHeader = hasPersonCol
                ? "<th style='padding:8px 12px;text-align:left;color:#374151;font-size:.8rem;font-weight:700;'>Person</th>"
                : "";

            sections.Append($"""
                <div style="margin-bottom:1.5rem;">
                  <table cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;">
                    <tr style="background:#dbeafe;">
                      <td colspan="5" style="padding:10px 12px;font-weight:800;color:#1d4ed8;font-size:.85rem;letter-spacing:.04em;text-transform:uppercase;">🎪 {group.Key}</td>
                    </tr>
                    <tr style="background:#eff6ff;border-bottom:2px solid #bfdbfe;">
                      <th style="padding:8px 12px;text-align:left;color:#374151;font-size:.8rem;font-weight:700;">Bereich</th>
                      <th style="padding:8px 12px;text-align:left;color:#374151;font-size:.8rem;font-weight:700;">Datum</th>
                      <th style="padding:8px 12px;text-align:left;color:#374151;font-size:.8rem;font-weight:700;">Zeit</th>
                      <th style="padding:8px 12px;text-align:left;color:#374151;font-size:.8rem;font-weight:700;">Ort</th>
                      {personHeader}
                    </tr>
                    {rows}
                  </table>
                </div>
                """);
        }

        var totalCount = appointments.Count;
        var body = $"""
            <div style="font-family:sans-serif;max-width:600px;margin:0 auto;">
              <div style="background:linear-gradient(135deg,#1d4ed8,#2563eb);border-radius:16px 16px 0 0;padding:28px 32px;">
                <h1 style="margin:0;color:#fff;font-size:1.5rem;">📋 Einsatz-Übersicht</h1>
                <p style="margin:.5rem 0 0;color:#bfdbfe;font-size:.95rem;">{totalCount} aktive Anmeldung{(totalCount == 1 ? "" : "en")}</p>
              </div>
              <div style="background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 16px 16px;padding:28px 32px;">
                <p style="color:#374151;margin-top:0;">Hallo <strong>{toName}</strong>,<br>hier ist deine aktuelle Übersicht aller angemeldeten Einsätze.</p>
                {sections}
                <p style="color:#64748b;font-size:.9rem;margin-top:1rem;">Bei Fragen kannst du dich direkt beim Flomi-Team melden.</p>
                <p style="color:#374151;font-weight:700;margin-bottom:0;">Freundliche Grüsse, das Flomi-Team 🙌</p>
              </div>
            </div>
            """;

        await SendAsync(toEmail, toName, subject, body);
    }

    // ─── Erinnerung am Vortag ─────────────────────────────────────────────────
    public async Task SendDayBeforeReminderAsync(string toEmail, string toName, ReminderMail mail)
    {
        var (subject, body) = BuildDayBeforeReminder(mail);
        await SendAsync(toEmail, toName, subject, body);
    }

    public static (string Subject, string Body) BuildDayBeforeReminder(ReminderMail mail)
    {
        var swiss    = new System.Globalization.CultureInfo("de-CH");
        var dayText  = mail.Date.ToString("dddd dd.MM.", swiss);
        var onlySpontaneous = mail.Items.All(i => i.IsSpontaneous);

        var subject = mail.Items.Count == 1 && !mail.Items[0].IsSpontaneous
            ? $"⏰ Morgen: {mail.Items[0].AreaName} {mail.Items[0].TimeSlot} · {mail.EventName}"
            : onlySpontaneous
                ? $"⏰ Morgen ist {mail.EventName} – wir freuen uns auf dich"
                : $"⏰ Morgen: {mail.Items.Count} Einsätze · {mail.EventName}";
        if (mail.IsTest) subject = "[Test] " + subject;

        var rows = new System.Text.StringBuilder();
        foreach (var item in mail.Items)
        {
            var title = Html(item.AreaName)
                + (string.IsNullOrEmpty(item.ForPerson) ? "" : $" <span style='color:#854d0e;'>({Html(item.ForPerson)})</span>");
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Location)) details.Add($"📍 {Html(item.Location)}");
            if (!string.IsNullOrWhiteSpace(item.VehicleInfo)) details.Add($"🚚 {Html(item.VehicleInfo)}");
            if (!string.IsNullOrWhiteSpace(item.Comment)) details.Add($"💬 <i>{Html(item.Comment)}</i>");

            rows.Append($"""
                <tr>
                  <td style="padding:12px;border-top:1px solid #e2e8f0;vertical-align:top;">
                    <div style="font-weight:800;color:#0f172a;">{title}</div>
                    <div style="color:#475569;font-size:.9rem;margin-top:2px;">{string.Join("<br>", details)}</div>
                    {(item.WithQrCode ? QrCodeBlock(item.AppointmentId) : "")}
                  </td>
                  <td style="padding:12px;border-top:1px solid #e2e8f0;vertical-align:top;text-align:right;white-space:nowrap;font-weight:700;color:#1d4ed8;">{Html(item.TimeSlot)}</td>
                </tr>
                """);
        }

        var intro = onlySpontaneous
            ? $"morgen, {Html(dayText)}, ist {Html(mail.EventName)}. Du hast angekündigt, spontan vorbeizukommen – wir freuen uns!"
            : $"morgen, {Html(dayText)}, bist du eingetragen:";
        var button = string.IsNullOrEmpty(mail.DashboardUrl) ? "" :
            $"<p style='margin:20px 0 0;'><a href='{Html(mail.DashboardUrl)}' style='display:inline-block;background:#1d4ed8;color:#fff;text-decoration:none;font-weight:700;padding:10px 18px;border-radius:10px;'>Im Dashboard ansehen</a></p>";
        var testNote = mail.IsTest
            ? "<p style='margin:0 0 16px;padding:8px 12px;background:#fef9c3;border-radius:8px;color:#854d0e;font-size:.85rem;'>Testmail – so sieht die Erinnerung am Vorabend aus.</p>"
            : "";

        var body = $"""
            <div style="font-family:sans-serif;max-width:540px;margin:0 auto;">
              <div style="background:linear-gradient(135deg,#1d4ed8,#2563eb);border-radius:16px 16px 0 0;padding:24px 32px;">
                <h1 style="margin:0;color:#fff;font-size:1.4rem;">Bis morgen! ⏰</h1>
              </div>
              <div style="background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 16px 16px;padding:24px 32px;">
                {testNote}
                <p style="color:#374151;margin-top:0;">Hallo <strong>{Html(mail.FirstName)}</strong>,<br>{intro}</p>
                <table cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;background:#fff;border:1px solid #e2e8f0;border-radius:12px;">
                  {rows}
                </table>
                {button}
                <p style="color:#374151;font-weight:700;margin-bottom:0;">Danke für deinen Einsatz! 🙌</p>
              </div>
            </div>
            """;

        return (subject, body);
    }

    private static string QrCodeBlock(int appointmentId)
    {
        var qrData = new QRCodeGenerator().CreateQrCode($"flomi-apt:{appointmentId}", QRCodeGenerator.ECCLevel.Q);
        var png    = Convert.ToBase64String(new PngByteQRCode(qrData).GetGraphic(6));
        return $"""
            <div style="margin-top:10px;">
              <img src="data:image/png;base64,{png}" style="width:140px;height:140px;border-radius:8px;" alt="QR-Code" />
              <div style="font-size:.78rem;color:#64748b;">Check-in-Code für den Verkauf</div>
            </div>
            """;
    }

    // ─── Admin-Mails ──────────────────────────────────────────────────────────
    // Empfänger und Inhalt stellt der AdminNotificationService zusammen. Alle Werte stammen
    // aus Eingaben der Benutzer und werden deshalb HTML-kodiert.
    public async Task SendAdminNewUserAsync(string toEmail, AdminNewUserMail mail)
    {
        var (subject, body) = BuildAdminNewUser(mail);
        await SendAsync(toEmail, "Admin", subject, body);
    }

    public async Task SendAdminRegistrationAsync(string toEmail, AdminRegistrationMail mail)
    {
        var (subject, body) = BuildAdminRegistration(mail);
        await SendAsync(toEmail, "Admin", subject, body);
    }

    public static (string Subject, string Body) BuildAdminNewUser(AdminNewUserMail mail)
    {
        var subject = $"👤 Neues Konto – {mail.FullName}";
        var rows = new List<(string, string?)>
        {
            ("Name",          mail.FullName),
            ("Pfadiname",     mail.Pfadiname),
            ("Stufe",         mail.Stufe),
            ("Geburtstag",    BirthdayWithAge(mail.Birthday)),
            ("E-Mail",        mail.Email + (mail.EmailConfirmed ? " (bestätigt)" : " (noch nicht bestätigt)")),
            ("Telefon",       mail.PhoneNumber),
            ("Registriert über", mail.Method),
            ("Zeitpunkt",     FormatSwiss(mail.CreatedAtUtc)),
            ("Konten total",  mail.TotalUsers.ToString()),
        };

        var body = AdminCard("#1d4ed8", "Neues Konto 👤",
            "Jemand hat sich neu bei der FlomiApp registriert.",
            "Konto", rows, mail.AdminUrl, "Zur Benutzerverwaltung");

        return (subject, body);
    }

    public static (string Subject, string Body) BuildAdminRegistration(AdminRegistrationMail mail)
    {
        var subject = mail.Cancelled
            ? $"❌ Stornierung – {mail.AreaName} · {mail.PersonName}"
            : $"📝 Neue Anmeldung – {mail.AreaName} · {mail.PersonName}";

        var area = string.IsNullOrEmpty(mail.CategoryName) || mail.CategoryName == mail.AreaName
            ? mail.AreaName
            : $"{mail.AreaName} ({mail.CategoryName})";
        var free = Math.Max(0, mail.Capacity - mail.Registered);

        var rows = new List<(string, string?)>
        {
            ("Person",     mail.PersonName),
            ("Stufe",      mail.PersonStufe),
            ("Geburtstag", BirthdayWithAge(mail.PersonBirthday)),
            ("Event",      mail.EventName),
            ("Bereich",    area),
            ("Datum",      AreaSchedule.FormatDate(mail.Date, "dddd, dd. MMMM yyyy")),
            ("Zeit",       mail.TimeSlot),
            ("Kommentar",  mail.Cancelled ? null : mail.Comment),
            ("Belegung",   $"{mail.Registered} von {mail.Capacity} Plätzen ({free} frei)"),
            ("Konto",      mail.ForFamilyMember ? $"{mail.AccountName} (meldet Familienmitglied an)" : mail.AccountName),
            ("E-Mail",     mail.AccountEmail),
            ("Telefon",    mail.AccountPhone),
            ("Anmeldungen im Event", $"{mail.AccountRegistrationsInEvent} aktiv (ganzes Konto inkl. Familie)"),
            ("Zeitpunkt",  FormatSwiss(mail.AtUtc)),
        };

        var body = mail.Cancelled
            ? AdminCard("#b91c1c", "Anmeldung storniert ❌",
                "Eine Anmeldung wurde storniert. Der Platz ist wieder frei.",
                "Stornierte Anmeldung", rows, mail.AdminUrl, "Zu den Anmeldungen")
            : AdminCard("#15803d", "Neue Anmeldung 📝",
                "Für einen Bereich ist eine neue Anmeldung eingegangen.",
                "Anmeldung", rows, mail.AdminUrl, "Zu den Anmeldungen");

        return (subject, body);
    }

    private static string AdminCard(string color, string title, string intro, string tableTitle,
        IEnumerable<(string Label, string? Value)> rows, string? linkUrl, string linkText)
    {
        var tableRows = new System.Text.StringBuilder();
        var odd = false;
        foreach (var (label, value) in rows)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            var bg = odd ? " style='background:#f8fafc;'" : "";
            tableRows.Append($"<tr{bg}><td style='padding:6px 12px;color:#64748b;width:40%;vertical-align:top;'>{Html(label)}:</td>" +
                             $"<td style='padding:6px 12px;font-weight:700;white-space:pre-line;'>{Html(value)}</td></tr>");
            odd = !odd;
        }

        var link = string.IsNullOrEmpty(linkUrl) ? "" :
            $"<p style='margin:20px 0 0;'><a href='{Html(linkUrl)}' style='display:inline-block;background:{color};color:#fff;text-decoration:none;font-weight:700;padding:10px 18px;border-radius:10px;'>{Html(linkText)}</a></p>";

        return $"""
            <div style="font-family:sans-serif;max-width:540px;margin:0 auto;">
              <div style="background:{color};border-radius:16px 16px 0 0;padding:24px 32px;">
                <h1 style="margin:0;color:#fff;font-size:1.4rem;">{Html(title)}</h1>
              </div>
              <div style="background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 16px 16px;padding:24px 32px;">
                <p style="color:#374151;margin-top:0;">{Html(intro)}</p>
                <table cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;margin:16px 0 0;background:#fff;border:1px solid #e2e8f0;">
                  <tr style="background:#f1f5f9;">
                    <td colspan="2" style="padding:10px 12px;font-weight:800;color:{color};font-size:.85rem;letter-spacing:.04em;text-transform:uppercase;">{Html(tableTitle)}</td>
                  </tr>
                  {tableRows}
                </table>
                {link}
                <p style="color:#94a3b8;font-size:.8rem;margin:20px 0 0;">Diese Mails kannst du in der App unter Einstellungen → Profil → E-Mail Einstellungen abschalten.</p>
              </div>
            </div>
            """;
    }

    private static string Html(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");

    private static string FormatSwiss(DateTime utc) =>
        TimeHelper.ToSwissTime(utc).ToString("dd.MM.yyyy, HH:mm 'Uhr'", new System.Globalization.CultureInfo("de-CH"));

    private static string? BirthdayWithAge(DateTime? birthday)
    {
        if (birthday == null) return null;
        var today = TimeHelper.ToSwissTime(DateTime.UtcNow).Date;
        var age   = today.Year - birthday.Value.Year;
        if (birthday.Value.Date > today.AddYears(-age)) age--;
        return $"{birthday.Value:dd.MM.yyyy} ({age} Jahre)";
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

    // ─── E-Mail-Bestätigung ───────────────────────────────────────────────────
    public async Task SendEmailConfirmationAsync(
        string toEmail, string toName, string confirmLink)
    {
        var subject = "Bitte bestätige deine E-Mail-Adresse";
        var body = $"""
            <h2>Hallo {toName}</h2>
            <p>Danke für deine Registrierung bei der FlomiApp! Bitte bestätige deine E-Mail-Adresse, damit wir sicher sind, dass sie dir gehört.</p>
            <p>
                <a href="{confirmLink}"
                   style="background:#007bff;color:white;padding:10px 20px;
                          text-decoration:none;border-radius:5px;">
                    E-Mail-Adresse bestätigen
                </a>
            </p>
            <p style="color:gray;font-size:12px;">
                Falls du dich nicht registriert hast, ignoriere diese Mail einfach.
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