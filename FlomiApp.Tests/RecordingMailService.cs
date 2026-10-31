using FlomiApp.Data.Models;
using FlomiApp.Services;

namespace FlomiApp.Tests;

/// <summary>Fake IMailService: hält die Admin-Mails fest, alles andere wird ignoriert.</summary>
public class RecordingMailService : IMailService
{
    public List<(string To, AdminNewUserMail Mail)>      NewUserMails      { get; } = new();
    public List<(string To, AdminRegistrationMail Mail)> RegistrationMails { get; } = new();
    public List<(string To, ReminderMail Mail)>          Reminders         { get; } = new();

    public Task SendDayBeforeReminderAsync(string toEmail, string toName, ReminderMail mail)
    {
        Reminders.Add((toEmail, mail));
        return Task.CompletedTask;
    }

    public Task SendAdminNewUserAsync(string toEmail, AdminNewUserMail mail)
    {
        NewUserMails.Add((toEmail, mail));
        return Task.CompletedTask;
    }

    public Task SendAdminRegistrationAsync(string toEmail, AdminRegistrationMail mail)
    {
        RegistrationMails.Add((toEmail, mail));
        return Task.CompletedTask;
    }

    public Task SendRegistrationConfirmationAsync(string toEmail, string toName, string areaName, string eventName,
        DateTime? date, string timeSlot, string? comment = null, int? appointmentId = null, string? forPerson = null)
        => Task.CompletedTask;

    public Task SendCancellationConfirmationAsync(string toEmail, string toName, string areaName, string eventName,
        DateTime? date, string timeSlot) => Task.CompletedTask;

    public Task SendAppointmentSummaryAsync(string toEmail, string toName, List<AppointmentSummaryItem> appointments)
        => Task.CompletedTask;

    public Task SendEventAnnouncementAsync(List<string> toEmails, string eventName, DateTime eventDate, string description)
        => Task.CompletedTask;

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink) => Task.CompletedTask;

    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink) => Task.CompletedTask;

    public Task SendAsync(string toEmail, string toName, string subject, string htmlBody) => Task.CompletedTask;
}
