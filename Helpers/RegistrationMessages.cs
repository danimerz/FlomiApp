using System.Globalization;
using FlomiApp.Services;

namespace FlomiApp.Helpers;

/// <summary>
/// Texte, warum eine Anmeldung nicht geht – für die Anmeldeseite und die Fehlermeldung des Services.
/// <paramref name="personFirstName"/> ist der Vorname eines Familienmitglieds; <c>null</c> heisst «du».
/// </summary>
public static class RegistrationMessages
{
    private static readonly CultureInfo Swiss = new("de-CH");

    public static string Describe(RegistrationCheck check, string? personFirstName = null)
    {
        var self = string.IsNullOrWhiteSpace(personFirstName);
        var who  = self ? "Du" : personFirstName!;
        var is_  = self ? "bist" : "ist";
        var has  = self ? "hast" : "hat";

        return check.Block switch
        {
            RegistrationBlock.None              => string.Empty,
            RegistrationBlock.AreaNotFound      => "Diesen Bereich gibt es nicht mehr.",
            RegistrationBlock.AlreadyRegistered => $"✓ {who} {is_} hier bereits angemeldet.",
            RegistrationBlock.FullDayOnly       => self
                ? "Für deine Stufe gibt es hier nur den ganzen Tag."
                : $"Für {who}s Stufe gibt es hier nur den ganzen Tag.",
            RegistrationBlock.MinAge            => MinAgeText(check, who, is_, self),
            RegistrationBlock.Full              => "Alle Plätze sind vergeben.",
            RegistrationBlock.TimeConflict      =>
                $"{who} {is_} zu dieser Zeit schon für «{check.OtherAreaName}» eingetragen{When(check)}. " +
                "Beides gleichzeitig geht nicht.",
            RegistrationBlock.SaleLimit         =>
                $"{who} {has} bereits den Verkaufsbereich «{check.OtherAreaName}». Pro Event ist nur einer möglich.",
            RegistrationBlock.CakeLimit         =>
                $"{who} {(self ? "bringst" : "bringt")} an diesem Tag schon einen Kuchen («{check.OtherAreaName}»).",
            _                                   => "Anmeldung nicht möglich.",
        };
    }

    /// <summary>Gründe, die man durch Stornieren einer anderen Anmeldung lösen kann.</summary>
    public static bool CanSwitch(RegistrationCheck check) =>
        check.Block is RegistrationBlock.TimeConflict or RegistrationBlock.SaleLimit or RegistrationBlock.CakeLimit;

    private static string MinAgeText(RegistrationCheck check, string who, string is_, bool self)
    {
        var text = $"Ab {check.MinAge} Jahren";
        return check.PersonAge is { } age
            ? $"{text} – {(self ? "du bist" : $"{who} {is_}")} {age}."
            : $"{text}.";
    }

    // « (Sa 10.04., 08:00-12:00)»
    private static string When(RegistrationCheck check)
    {
        var parts = new List<string>();
        if (check.OtherDate is { } date) parts.Add(date.ToString("ddd dd.MM.", Swiss));
        if (!string.IsNullOrWhiteSpace(check.OtherTimeSlot)) parts.Add(check.OtherTimeSlot!);
        return parts.Count == 0 ? string.Empty : $" ({string.Join(", ", parts)})";
    }
}
