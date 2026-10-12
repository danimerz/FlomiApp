using System.ComponentModel.DataAnnotations;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlomiApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginWithRecoveryCodeModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte einen Recovery-Code eingeben.")]
        [Display(Name = "Recovery-Code")]
        public string RecoveryCode { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return Page();

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        var cleanCode = Input.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(cleanCode);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in with a recovery code.");
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "~/" : returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out after too many recovery code attempts.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Ungültiger oder bereits verwendeter Recovery-Code.");
        return Page();
    }
}
