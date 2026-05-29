using System.ComponentModel.DataAnnotations;
using System.Text;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FlomiApp.Areas.Identity.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? userId = null, string? code = null)
    {
        if (userId == null || code == null)
            return RedirectToPage("/Account/Login");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return RedirectToPage("/Account/Login");

        Input = new InputModel
        {
            Code   = code,
            UserId = userId,
            Email  = user.Email ?? string.Empty
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByIdAsync(Input.UserId);
        if (user == null)
            return RedirectToPage("/Account/Login");

        // Token dekodieren (Base64Url → UTF8)
        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Ungültiger oder abgelaufener Reset-Link.");
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);
        if (result.Succeeded)
            return RedirectToPage("ResetPasswordConfirmation");

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
