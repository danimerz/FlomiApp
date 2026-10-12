using System.Security.Claims;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FlomiApp.Security;

public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));
            identity.AddClaim(new Claim("FirstName", user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));
            identity.AddClaim(new Claim("LastName", user.LastName));
        }

        if (!string.IsNullOrWhiteSpace(user.Pfadiname))
        {
            identity.AddClaim(new Claim("Pfadiname", user.Pfadiname));
        }

        // Für die 2FA-Pflicht der Admin-Rollen (RequireTwoFactorForAdmins-Policy):
        // wird in den Cookie gebacken, damit die Policy ohne DB-Zugriff pro Request prüfen kann.
        // Nach Ein-/Ausschalten von 2FA muss SignInManager.RefreshSignInAsync aufgerufen werden,
        // damit der Claim im aktuellen Cookie aktualisiert wird.
        identity.AddClaim(new Claim("tfa_enrolled", user.TwoFactorEnabled ? "true" : "false"));

        return identity;
    }
}