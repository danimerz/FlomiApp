using System.Globalization;
using System.Security.Claims;

namespace FlomiApp.Security;

/// <summary>
/// Profildaten, die ein externer Anbieter (Google, Microsoft, MiData) beim Login liefert.
/// Die Anbieter benennen ihre Claims unterschiedlich – hier wird das vereinheitlicht.
/// </summary>
public sealed record ExternalLoginProfile(
    string?   Email,
    bool      EmailTrusted,
    string?   FirstName,
    string?   LastName,
    string?   Pfadiname,
    DateTime? Birthday)
{
    public const string EmailVerifiedClaim = "email_verified";

    private static readonly string[] BirthdayFormats = { "yyyy-MM-dd", "dd.MM.yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" };

    public static ExternalLoginProfile FromPrincipal(ClaimsPrincipal principal, bool providerTrustsEmail)
    {
        var email = First(principal, ClaimTypes.Email, "email");

        // Ein ausdrücklich unbestätigtes "email_verified" überstimmt die Anbieter-Einstellung
        var verified     = First(principal, EmailVerifiedClaim);
        var emailTrusted = providerTrustsEmail
                           && !string.IsNullOrWhiteSpace(email)
                           && (verified is null || string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase));

        return new ExternalLoginProfile(
            Email:        email,
            EmailTrusted: emailTrusted,
            FirstName:    First(principal, "first_name", ClaimTypes.GivenName, "given_name"),
            LastName:     First(principal, "last_name", ClaimTypes.Surname, "family_name"),
            Pfadiname:    First(principal, "nickname"),
            Birthday:     ParseBirthday(First(principal, "birthday", ClaimTypes.DateOfBirth)));
    }

    private static string? First(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }
        return null;
    }

    private static DateTime? ParseBirthday(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParseExact(value, BirthdayFormats, CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out var date)
            ? date.Date
            : null;
    }
}
