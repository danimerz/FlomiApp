namespace FlomiApp.Helpers;

/// <summary>
/// Wann die Erinnerung am Vortag fällig ist: am Vorabend des Einsatzes um 18:00 Schweizer Zeit.
/// </summary>
public static class ReminderSchedule
{
    public static readonly TimeSpan SendTime = new(18, 0, 0);

    /// <summary>Versandzeitpunkt (Schweizer Zeit) für einen Einsatz an <paramref name="areaDate"/>.</summary>
    public static DateTime SendMomentFor(DateTime areaDate) => areaDate.Date.AddDays(-1) + SendTime;

    /// <summary>
    /// Ob der Versandzeitpunkt schon vorbei ist. Wer sich danach anmeldet, bekommt keine Erinnerung –
    /// die Bestätigungsmail kam ja gerade.
    /// </summary>
    public static bool IsPastSendMoment(DateTime areaDate, DateTime utcNow) =>
        TimeHelper.ToSwissTime(utcNow) >= SendMomentFor(areaDate);

    /// <summary>
    /// Für welchen Einsatztag jetzt Erinnerungen verschickt werden: ab 18:00 für morgen, sonst keinen.
    /// </summary>
    public static DateTime? DueAreaDate(DateTime utcNow)
    {
        var swissNow = TimeHelper.ToSwissTime(utcNow);
        return swissNow.TimeOfDay >= SendTime ? swissNow.Date.AddDays(1) : null;
    }
}
