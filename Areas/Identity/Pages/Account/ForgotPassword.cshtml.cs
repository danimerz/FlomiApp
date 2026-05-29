using System.ComponentModel.DataAnnotations;
using System.Text;
using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FlomiApp.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMailService                 _mailService;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IMailService mailService)
    {
        _userManager = userManager;
        _mailService  = mailService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool EmailSent { get; set; } = false;

    public class InputModel
    {
        [Required(ErrorMessage = "Bitte E-Mail eingeben.")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);

        // Aus Sicherheitsgründen immer die Bestätigungsseite zeigen,
        // auch wenn kein Konto existiert (verhindert User-Enumeration)
        if (user != null)
        {
            var token   = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink = $"{Request.Scheme}://{Request.Host}/Identity/Account/ResetPassword" +
                            $"?userId={user.Id}&code={encoded}";

            await _mailService.SendPasswordResetAsync(
                user.Email!,
                $"{user.FirstName} {user.LastName}",
                resetLink);
        }

        EmailSent = true;
        return Page();
    }
}
