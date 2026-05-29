namespace FlomiApp.Helpers;

public static class InputHelper
{
    /// <summary>Normalizes any Swiss phone number to +41 XX XXX XX XX.</summary>
    public static string? FormatSwissPhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if      (digits.StartsWith("0041"))                            digits = digits[4..];
        else if (digits.StartsWith("41") && digits.Length == 11)       digits = digits[2..];
        else if (digits.StartsWith("0"))                               digits = digits[1..];

        if (digits.Length != 9) return input;

        return $"+41 {digits[0..2]} {digits[2..5]} {digits[5..7]} {digits[7..9]}";
    }

    /// <summary>
    /// Normalizes a single time entry to HH:mm.
    /// Accepts: "730", "0730", "7:30", "7.30" → "07:30".
    /// Returns input unchanged if it cannot be parsed.
    /// </summary>
    public static string? FormatTime(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var clean = input.Trim().Replace(".", ":").Replace(",", ":");

        // Already HH:mm or H:mm
        if (clean.Contains(':'))
        {
            var p = clean.Split(':');
            if (p.Length == 2 && int.TryParse(p[0], out var h) && int.TryParse(p[1], out var m)
                && h <= 23 && m <= 59)
                return $"{h:D2}:{m:D2}";
        }

        // 3 or 4 digits: 730 or 0730
        var digits = new string(clean.Where(char.IsDigit).ToArray());
        if (digits.Length is 3 or 4)
        {
            var padded = digits.PadLeft(4, '0');
            if (int.TryParse(padded[0..2], out var hh) && int.TryParse(padded[2..4], out var mm)
                && hh <= 23 && mm <= 59)
                return $"{hh:D2}:{mm:D2}";
        }

        return input;
    }

    /// <summary>
    /// Normalizes a time-range slot to HH:mm-HH:mm.
    /// Accepts: "0730-1630", "7:30-16:30" → "07:30-16:30".
    /// Falls back to FormatTime for single values.
    /// </summary>
    public static string? FormatTimeSlot(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var idx = input.IndexOf('-');
        if (idx > 0)
        {
            var from = FormatTime(input[..idx].Trim());
            var to   = FormatTime(input[(idx + 1)..].Trim());
            if (from != null && to != null)
                return $"{from}-{to}";
        }

        return FormatTime(input);
    }
}
