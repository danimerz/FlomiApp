using FlomiApp.Data.Models;

namespace FlomiApp.Helpers;

/// <summary>Anzeige von Datum und Zeit eines Bereichs – inkl. spontaner Bereiche ohne fixe Zeit.</summary>
public static class AreaSchedule
{
    public const string SpontaneousDate = "Spontan, ohne fixes Datum";
    public const string SpontaneousTime = "komme spontan vorbei";

    /// <summary>Gebuchter Zeitslot (ganzer oder halber Tag) bzw. «komme spontan vorbei».</summary>
    public static string TimeSlot(Area? area, bool useAlternativeSlot = false)
    {
        if (area == null) return string.Empty;
        if (area.IsSpontaneous) return SpontaneousTime;
        return useAlternativeSlot && !string.IsNullOrEmpty(area.AlternativeTimeSlot)
            ? area.AlternativeTimeSlot
            : area.TimeSlot;
    }

    /// <summary>Datum für Mails und Übersichten; <c>null</c> bei spontanen Bereichen.</summary>
    public static DateTime? Date(Area? area) =>
        area == null || area.IsSpontaneous ? null : area.Date;

    public static string FormatDate(DateTime? date, string format) =>
        date.HasValue
            ? date.Value.ToString(format, new System.Globalization.CultureInfo("de-CH"))
            : SpontaneousDate;
}
