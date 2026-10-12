using System.ComponentModel.DataAnnotations;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlomiApp.Areas.Identity.Pages.Account;

[Authorize]
public class ChangePasswordRequiredModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ChangePasswordRequiredModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte aktuelles Passwort eingeben.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte neues Passwort eingeben.")]
        [StringLength(100, ErrorMessage = "{0} muss mindestens {2} Zeichen lang sein.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("./Login");

        if (!user.MustChangePassword)
            return LocalRedirect("~/");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage("./Login");

        if (!ModelState.IsValid) return Page();

        var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        // Security-Stamp ändert sich durch ChangePasswordAsync bereits;
        // Sign-in trotzdem auffrischen, damit die aktuelle Session sauber weiterläuft.
        await _signInManager.RefreshSignInAsync(user);

        return LocalRedirect("~/");
    }
}
