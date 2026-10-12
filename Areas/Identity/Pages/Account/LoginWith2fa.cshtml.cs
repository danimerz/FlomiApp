using System.ComponentModel.DataAnnotations;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlomiApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginWith2faModel> _logger;

    public LoginWith2faModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginWith2faModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte den 6-stelligen Code eingeben.")]
        [StringLength(7, MinimumLength = 6, ErrorMessage = "Der Code hat 6 Ziffern.")]
        public string Code { get; set; } = string.Empty;

        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        if (!ModelState.IsValid) return Page();

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        var cleanCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            cleanCode, rememberMe, Input.RememberMachine);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in with 2FA.");
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "~/" : returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out after too many 2FA attempts.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Ungültiger Code. Bitte erneut versuchen.");
        return Page();
    }
}
