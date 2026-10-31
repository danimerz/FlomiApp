using System.ComponentModel.DataAnnotations;
using System.Text;
using FlomiApp.Data.Models;
using FlomiApp.Security;
using FlomiApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FlomiApp.Areas.Identity.Pages.Account;

/// <summary>
/// Anmeldung über Google, Microsoft oder MiData.
/// Ablauf: Login-Button (POST) → Anbieter → Callback. Ist das externe Konto schon verknüpft,
/// wird direkt angemeldet; sonst wird ein bestehendes Konto mit bestätigter E-Mail verknüpft
/// oder die Person vervollständigt ihr Profil (Stufe, Geburtstag, Telefon fehlen bei den Anbietern).
/// Admin-Konten melden sich bewusst nur mit Passwort + 2FA an.
/// </summary>
[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ExternalLoginSettings _externalSettings;
    private readonly IMailQueue _mailQueue;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly ILogger<ExternalLoginModel> _logger;

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ExternalLoginSettings externalSettings,
        IMailQueue mailQueue,
        IAdminNotificationService adminNotifications,
        ILogger<ExternalLoginModel> logger)
    {
        _signInManager      = signInManager;
        _userManager        = userManager;
        _externalSettings   = externalSettings;
        _mailQueue          = mailQueue;
        _adminNotifications = adminNotifications;
        _logger             = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ProviderDisplayName { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;

    // Ist die E-Mail vom Anbieter bestätigt, entfällt die Bestätigungsmail
    public bool EmailTrusted { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse an.")]
        [EmailAddress(ErrorMessage = "Bitte gib eine gültige E-Mail-Adresse an.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte gib deinen Vornamen an.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte gib deinen Nachnamen an.")]
        public string LastName { get; set; } = string.Empty;

        public string? Pfadiname { get; set; }

        [Required(ErrorMessage = "Bitte wähle deine Stufe.")]
        public string Stufe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte gib deinen Geburtstag an.")]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Required(ErrorMessage = "Bitte gib deine Telefonnummer an.")]
        [Phone(ErrorMessage = "Bitte gib eine gültige Telefonnummer an.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public IActionResult OnGet() => RedirectToPage("./Login");

    // Klick auf «Mit Google anmelden» usw.
    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties  = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl = SafeReturnUrl(returnUrl);

        if (remoteError != null)
        {
            // Details stehen bereits im Log (ExternalAuthSetup.HandleRemoteFailure)
            return LoginError(remoteError == "cancelled"
                ? "Die Anmeldung beim externen Dienst wurde abgebrochen."
                : "Die Anmeldung beim externen Dienst hat nicht geklappt. Bitte versuche es erneut " +
                  "oder melde dich mit E-Mail und Passwort an.", returnUrl);
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return LoginError("Die Anmeldedaten des externen Dienstes konnten nicht gelesen werden. Bitte versuche es erneut.", returnUrl);

        // 1) Externes Konto ist bereits verknüpft
        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user != null)
            return await SignInLinkedUserAsync(user, info, returnUrl);

        var profile = ExternalLoginProfile.FromPrincipal(
            info.Principal, _externalSettings.IsEmailTrusted(info.LoginProvider));

        // 2) Konto mit derselben E-Mail existiert schon
        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            var existing = await _userManager.FindByEmailAsync(profile.Email);
            if (existing != null)
            {
                if (await IsAdminAsync(existing))
                    return LoginError(AdminNotAllowedMessage(info.ProviderDisplayName), returnUrl);

                if (!profile.EmailTrusted)
                {
                    return LoginError(
                        $"Es gibt bereits ein Konto mit {profile.Email}. Melde dich mit deinem Passwort an " +
                        $"und verknüpfe {info.ProviderDisplayName} danach unter Einstellungen → Sicherheit.",
                        returnUrl);
                }

                var linkResult = await _userManager.AddLoginAsync(existing, info);
                if (!linkResult.Succeeded)
                    return LoginError("Das externe Konto konnte nicht verknüpft werden.", returnUrl);

                // Der Anbieter hat die Adresse bestätigt – damit gilt sie auch hier als bestätigt
                if (!existing.EmailConfirmed)
                {
                    existing.EmailConfirmed = true;
                    await _userManager.UpdateAsync(existing);
                }

                _logger.LogInformation("Bestehendes Konto mit {Provider} verknüpft.", info.LoginProvider);
                return await SignInLinkedUserAsync(existing, info, returnUrl);
            }
        }

        // 3) Neues Konto: Profil vervollständigen lassen
        ReturnUrl           = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
        EmailTrusted        = profile.EmailTrusted;
        Input = new InputModel
        {
            Email     = profile.Email     ?? string.Empty,
            FirstName = profile.FirstName ?? string.Empty,
            LastName  = profile.LastName  ?? string.Empty,
            Pfadiname = profile.Pfadiname,
            Birthday  = profile.Birthday
        };
        return Page();
    }

    // Absenden von «Profil vervollständigen»
    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        returnUrl = SafeReturnUrl(returnUrl);

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return LoginError("Die Sitzung ist abgelaufen. Bitte melde dich erneut beim externen Dienst an.", returnUrl);

        var profile = ExternalLoginProfile.FromPrincipal(
            info.Principal, _externalSettings.IsEmailTrusted(info.LoginProvider));

        ReturnUrl           = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
        EmailTrusted        = profile.EmailTrusted;

        if (!ModelState.IsValid)
            return Page();

        // Bestätigt ist die Adresse nur, wenn sie unverändert vom Anbieter übernommen wurde
        var emailConfirmed = profile.EmailTrusted
                             && string.Equals(profile.Email, Input.Email, StringComparison.OrdinalIgnoreCase);

        var user = new ApplicationUser
        {
            UserName       = Input.Email,
            Email          = Input.Email,
            EmailConfirmed = emailConfirmed,
            FirstName      = Input.FirstName,
            LastName       = Input.LastName,
            Pfadiname      = Input.Pfadiname ?? string.Empty,
            Stufe          = Input.Stufe,
            Birthday       = Input.Birthday,
            PhoneNumber    = Input.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded)
            result = await _userManager.AddLoginAsync(user, info);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        _logger.LogInformation("Neues Konto über {Provider} erstellt.", info.LoginProvider);
        await _adminNotifications.NotifyNewUserAsync(user.Id, ProviderDisplayName);

        if (emailConfirmed)
        {
            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return LocalRedirect(returnUrl);
        }

        // Adresse unbestätigt (z.B. Microsoft oder geändert): wie bei der normalen Registrierung
        var token       = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded     = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmLink = $"{Request.Scheme}://{Request.Host}/Identity/Account/ConfirmEmail" +
                          $"?userId={user.Id}&code={encoded}";
        var fullName    = $"{user.FirstName} {user.LastName}";
        _mailQueue.Enqueue(m => m.SendEmailConfirmationAsync(user.Email!, fullName, confirmLink));

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return RedirectToPage("./RegisterConfirmation");
    }

    private async Task<IActionResult> SignInLinkedUserAsync(ApplicationUser user, ExternalLoginInfo info, string returnUrl)
    {
        if (await IsAdminAsync(user))
            return LoginError(AdminNotAllowedMessage(info.ProviderDisplayName), returnUrl);

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("Login über {Provider}.", info.LoginProvider);
            return LocalRedirect(returnUrl);
        }
        if (result.RequiresTwoFactor)
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl });
        if (result.IsLockedOut)
            return RedirectToPage("./Lockout");
        if (result.IsNotAllowed)
            return LoginError("Bitte bestätige zuerst deine E-Mail-Adresse (Link aus der Mail nach der Registrierung).", returnUrl);

        return LoginError("Die Anmeldung ist fehlgeschlagen. Bitte versuche es erneut.", returnUrl);
    }

    private async Task<bool> IsAdminAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(Roles.AllAdminRoles.Contains);
    }

    private static string AdminNotAllowedMessage(string? provider) =>
        $"Admin-Konten können sich nicht über {provider} anmelden. Bitte melde dich mit E-Mail, Passwort und 2FA an.";

    private IActionResult LoginError(string message, string returnUrl)
    {
        ErrorMessage = message;
        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
}
