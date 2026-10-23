using FlomiApp.Data.Models;
using FlomiApp.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlomiApp.Areas.Identity.Pages.Account;

/// <summary>
/// Verknüpft ein externes Konto (Google, Microsoft, MiData) mit dem angemeldeten Benutzer.
/// Aufgerufen aus Einstellungen → Sicherheit; die Blazor-Seite kann selbst keinen
/// Challenge-Redirect auslösen, deshalb läuft das über diese Razor-Seite.
/// </summary>
[Authorize]
public class LinkLoginModel : PageModel
{
    private const string SettingsUrl = "/user/settings?tab=security";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LinkLoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
    }

    public string Provider { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;

    // Bestätigungsseite – der eigentliche Start braucht einen POST mit Antiforgery-Token
    public async Task<IActionResult> OnGetAsync(string provider)
    {
        var scheme = (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .FirstOrDefault(s => s.Name == provider);
        if (scheme == null) return Redirect(SettingsUrl);

        Provider            = scheme.Name;
        ProviderDisplayName = scheme.DisplayName ?? scheme.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string provider)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("./Login");
        if (await IsAdminAsync(user)) return Redirect(SettingsUrl + "&linkError=admin");

        // Altes externes Cookie entfernen, damit sicher der neue Login verknüpft wird
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        var redirectUrl = Url.Page("./LinkLogin", pageHandler: "Callback");
        var properties  = _signInManager.ConfigureExternalAuthenticationProperties(
            provider, redirectUrl, _userManager.GetUserId(User));
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("./Login");

        var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
        if (info == null) return Redirect(SettingsUrl + "&linkError=failed");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        var alreadyUsed = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (alreadyUsed != null && alreadyUsed.Id != user.Id)
            return Redirect(SettingsUrl + "&linkError=taken");

        var result = await _userManager.AddLoginAsync(user, info);
        if (!result.Succeeded && alreadyUsed == null)
            return Redirect(SettingsUrl + "&linkError=failed");

        return Redirect(SettingsUrl + "&linked=" + Uri.EscapeDataString(info.ProviderDisplayName ?? info.LoginProvider));
    }

    private async Task<bool> IsAdminAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(Roles.AllAdminRoles.Contains);
    }
}
