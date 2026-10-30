using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace FlomiApp.Areas.Identity.Pages.Account;

[Authorize]
public class TwoFactorSetupModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UrlEncoder _urlEncoder;
    private readonly IPasskeyService _passkeyService;

    private const string Issuer = "FlomiApp";

    public TwoFactorSetupModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UrlEncoder urlEncoder,
        IPasskeyService passkeyService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _urlEncoder = urlEncoder;
        _passkeyService = passkeyService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte den 6-stelligen Code eingeben.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Der Code hat 6 Ziffern.")]
        public string Code { get; set; } = string.Empty;
    }

    public string? ReturnUrl { get; set; }
    public bool AlreadyEnabled { get; set; }
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string QrCodeDataUri { get; set; } = string.Empty;

    // Admins können statt der App auch einen Passkey nutzen (Hinweis auf der Seite)
    public bool HasPasskeys { get; set; }

    // Nach erfolgreicher Aktivierung einmalig angezeigt
    public bool JustEnabled { get; set; }
    public string[]? RecoveryCodes { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("./Login");

        if (user.TwoFactorEnabled)
        {
            AlreadyEnabled = true;
            return Page();
        }

        await LoadAuthenticatorKeyAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("./Login");

        if (!ModelState.IsValid)
        {
            await LoadAuthenticatorKeyAsync(user);
            return Page();
        }

        var cleanCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, cleanCode);

        if (!isCodeValid)
        {
            ModelState.AddModelError(string.Empty, "Der Code ist falsch oder abgelaufen. Bitte erneut versuchen.");
            await LoadAuthenticatorKeyAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        // Claims-Cookie sofort aktualisieren (tfa_enrolled-Claim), sonst bleibt
        // die Admin-Seite bis zum nächsten Login gesperrt.
        await _signInManager.RefreshSignInAsync(user);

        JustEnabled = true;
        RecoveryCodes = recoveryCodes?.ToArray();
        return Page();
    }

    private async Task LoadAuthenticatorKeyAsync(ApplicationUser user)
    {
        HasPasskeys = await _passkeyService.CountForUserAsync(user.Id) > 0;

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKeyForDisplay(unformattedKey!);

        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? "user";
        AuthenticatorUri =
            $"otpauth://totp/{_urlEncoder.Encode(Issuer)}:{_urlEncoder.Encode(email)}" +
            $"?secret={unformattedKey}&issuer={_urlEncoder.Encode(Issuer)}&digits=6";

        var qrGen = new QRCodeGenerator();
        var qrCodeData = qrGen.CreateQrCode(AuthenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrPng = qrCode.GetGraphic(6);
        QrCodeDataUri = $"data:image/png;base64,{Convert.ToBase64String(qrPng)}";
    }

    private static string FormatKeyForDisplay(string unformattedKey)
    {
        var sb = new StringBuilder();
        var pos = 0;
        while (pos + 4 < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(pos, 4)).Append(' ');
            pos += 4;
        }
        if (pos < unformattedKey.Length)
        {
            sb.Append(unformattedKey.AsSpan(pos));
        }
        return sb.ToString().ToUpperInvariant();
    }
}
