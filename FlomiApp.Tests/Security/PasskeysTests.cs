using System.Security.Claims;
using FlomiApp.Security;

namespace FlomiApp.Tests.Security;

public class PasskeysTests
{
    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test", ClaimTypes.Name, ClaimTypes.Role));

    [Fact]
    public void NonAdmin_AlwaysMeetsRequirement()
    {
        Assert.True(Passkeys.MeetsAdminTwoFactorRequirement(Principal(("amr", "pwd"))));
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.FahrzeugAdmin)]
    [InlineData(Roles.MoebelAdmin)]
    [InlineData(Roles.BereichsAdmin)]
    public void Admin_WithPasswordOnly_IsRejected(string role)
    {
        var admin = Principal((ClaimTypes.Role, role), ("tfa_enrolled", "false"), ("amr", "pwd"));
        Assert.False(Passkeys.MeetsAdminTwoFactorRequirement(admin));
    }

    [Fact]
    public void Admin_WithAuthenticatorApp_MeetsRequirement()
    {
        var admin = Principal((ClaimTypes.Role, Roles.Admin), ("tfa_enrolled", "true"), ("amr", "mfa"));
        Assert.True(Passkeys.MeetsAdminTwoFactorRequirement(admin));
    }

    [Fact]
    public void Admin_SignedInWithPasskey_MeetsRequirementWithoutApp()
    {
        var admin = Principal((ClaimTypes.Role, Roles.Admin), ("tfa_enrolled", "false"),
                              (Passkeys.AmrClaimType, Passkeys.AuthenticationMethod));
        Assert.True(Passkeys.IsPasskeySession(admin));
        Assert.True(Passkeys.MeetsAdminTwoFactorRequirement(admin));
    }

    [Theory]
    [InlineData(null, "Passkey")]
    [InlineData("   ", "Passkey")]
    [InlineData("  iPhone Dani ", "iPhone Dani")]
    public void NormalizeName_TrimsAndFallsBack(string? input, string expected)
    {
        Assert.Equal(expected, Passkeys.NormalizeName(input));
    }

    [Fact]
    public void NormalizeName_CutsLongNames()
    {
        Assert.Equal(Passkeys.MaxNameLength, Passkeys.NormalizeName(new string('x', 300)).Length);
    }
}
