namespace FlomiApp.Helpers;

public static class PhoneHelper
{
    /// <summary>
    /// Normalizes any Swiss phone number to +41 XX XXX XX XX.
    /// Returns the input unchanged if it cannot be parsed as a 9-digit Swiss number.
    /// </summary>
    public static string? FormatSwissPhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("0041"))                           digits = digits[4..];
        else if (digits.StartsWith("41") && digits.Length == 11) digits = digits[2..];
        else if (digits.StartsWith("0"))                         digits = digits[1..];

        if (digits.Length != 9) return input;

        return $"+41 {digits[0..2]} {digits[2..5]} {digits[5..7]} {digits[7..9]}";
    }
}
