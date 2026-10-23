using System.Security.Claims;
using FlomiApp.Security;

namespace FlomiApp.Tests.Security;

public class ExternalLoginProfileTests
{
    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));

    [Fact]
    public void Google_VerifiedEmail_IsTrusted()
    {
        var p = Principal(
            (ClaimTypes.Email, "max@example.com"),
            (ExternalLoginProfile.EmailVerifiedClaim, "true"),
            (ClaimTypes.GivenName, "Max"),
            (ClaimTypes.Surname, "Muster"));

        var profile = ExternalLoginProfile.FromPrincipal(p, providerTrustsEmail: true);

        Assert.Equal("max@example.com", profile.Email);
        Assert.True(profile.EmailTrusted);
        Assert.Equal("Max", profile.FirstName);
        Assert.Equal("Muster", profile.LastName);
    }

    [Fact]
    public void UnverifiedEmailClaim_OverridesProviderTrust()
    {
        var p = Principal(
            (ClaimTypes.Email, "max@example.com"),
            (ExternalLoginProfile.EmailVerifiedClaim, "false"));

        Assert.False(ExternalLoginProfile.FromPrincipal(p, providerTrustsEmail: true).EmailTrusted);
    }

    [Fact]
    public void ProviderWithoutTrust_NeverTrustsEmail()
    {
        var p = Principal(
            (ClaimTypes.Email, "max@example.com"),
            (ExternalLoginProfile.EmailVerifiedClaim, "true"));

        Assert.False(ExternalLoginProfile.FromPrincipal(p, providerTrustsEmail: false).EmailTrusted);
    }

    [Fact]
    public void MissingEmail_IsNotTrusted()
    {
        var p = Principal((ClaimTypes.GivenName, "Max"));

        var profile = ExternalLoginProfile.FromPrincipal(p, providerTrustsEmail: true);

        Assert.Null(profile.Email);
        Assert.False(profile.EmailTrusted);
    }

    [Fact]
    public void MiData_MapsPfadinameAndBirthday()
    {
        var p = Principal(
            ("email", "fuchs@pfadi.ch"),
            ("first_name", "Anna"),
            ("last_name", "Beispiel"),
            ("nickname", "Fuchs"),
            ("birthday", "2008-04-17"));

        var profile = ExternalLoginProfile.FromPrincipal(p, providerTrustsEmail: true);

        Assert.Equal("fuchs@pfadi.ch", profile.Email);
        Assert.Equal("Anna", profile.FirstName);
        Assert.Equal("Beispiel", profile.LastName);
        Assert.Equal("Fuchs", profile.Pfadiname);
        Assert.Equal(new DateTime(2008, 4, 17), profile.Birthday);
    }

    [Theory]
    [InlineData("17.04.2008")]
    [InlineData("2008-04-17T00:00:00")]
    public void Birthday_AcceptsCommonFormats(string value)
    {
        var profile = ExternalLoginProfile.FromPrincipal(Principal(("birthday", value)), false);

        Assert.Equal(new DateTime(2008, 4, 17), profile.Birthday);
    }

    [Fact]
    public void InvalidBirthday_IsIgnored()
    {
        var profile = ExternalLoginProfile.FromPrincipal(Principal(("birthday", "irgendwann")), false);

        Assert.Null(profile.Birthday);
    }
}
