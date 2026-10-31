namespace FlomiApp.Services;

/// <summary>
/// Erinnerung am Vortag: am Vorabend um 18:00 eine Mail pro Konto mit allen Einsätzen des nächsten
/// Tages. Ausgelöst von <see cref="ReminderBackgroundService"/>; pro Event abschaltbar
/// (<c>Event.RemindersEnabled</c>), pro Person über die Bestätigungs-Mails.
/// </summary>
public interface IReminderService
{
    /// <summary>Verschickt die fälligen Erinnerungen und gibt die Anzahl Mails zurück.</summary>
    Task<int> SendDueRemindersAsync(DateTime utcNow);

    /// <summary>Testmail an eine Person: ihre Einsätze im Event, sonst ein Beispiel aus dem Event.</summary>
    Task<bool> SendTestReminderAsync(string userId, int eventId);

    /// <summary>Für das Admin-Dashboard: Einsätze morgen und wie viele Personen schon erinnert wurden.</summary>
    Task<ReminderStatus> GetTomorrowStatusAsync(DateTime utcNow);
}

public record ReminderStatus(DateTime Date, int Appointments, int People, int PeopleReminded, bool SendTimePassed);
