namespace FlomiApp.Helpers;

/// <summary>
/// Welche Tage der Fahrzeug-Einsatzplan zeigt: der Abholzeitraum aus den Möbel-Einstellungen
/// des Events, ohne Abholzeitraum die 8 Tage vor dem Event bis zum Eventtag. Tage mit bereits
/// eingetragenen Fahrzeugen, Fahrern oder Abholungen kommen immer dazu, damit nichts verschwindet.
/// </summary>
public static class VehiclePlanDays
{
    public const int DefaultDaysBefore = 8;

    /// <summary>Obergrenze, damit ein Tippfehler im Abholzeitraum keine Riesentabelle erzeugt.</summary>
    public const int MaxDays = 60;

    public static List<DateTime> For(DateTime eventDate, DateTime? pickupFrom, DateTime? pickupTo,
                                     IEnumerable<DateTime>? usedDays = null)
    {
        var start = eventDate.Date.AddDays(-DefaultDaysBefore);
        var end   = eventDate.Date;

        if (pickupFrom is { } from)
        {
            start = from.Date;
            end   = pickupTo is { } to && to.Date >= from.Date ? to.Date : eventDate.Date;
            if (end < start) end = start;
        }

        foreach (var day in usedDays ?? Enumerable.Empty<DateTime>())
        {
            if (day.Date < start) start = day.Date;
            if (day.Date > end)   end   = day.Date;
        }

        var count = Math.Min((end - start).Days + 1, MaxDays);
        return Enumerable.Range(0, count).Select(i => start.AddDays(i)).ToList();
    }
}
