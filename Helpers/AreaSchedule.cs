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

    /// <summary>
    /// Ob diese Stufe hier nur den ganzen Tag buchen kann (<see cref="AreaTemplate.MandatoryStufen"/>,
    /// z.B. «Pfadi,Pio,Leiter»). Gilt für Anmeldeseite und Service gleich.
    /// </summary>
    public static bool IsFullDayOnly(Area? area, string? stufe)
    {
        var mandatory = area?.AreaTemplate?.MandatoryStufen;
        if (string.IsNullOrEmpty(mandatory) || string.IsNullOrEmpty(stufe)) return false;
        return mandatory
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(stufe, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Datum für Mails und Übersichten; <c>null</c> bei spontanen Bereichen.</summary>
    public static DateTime? Date(Area? area) =>
        area == null || area.IsSpontaneous ? null : area.Date;

    public static string FormatDate(DateTime? date, string format) =>
        date.HasValue
            ? date.Value.ToString(format, new System.Globalization.CultureInfo("de-CH"))
            : SpontaneousDate;
}
