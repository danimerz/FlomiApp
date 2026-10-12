using System.ComponentModel.DataAnnotations;
using System.Text;
using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FlomiApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMailQueue                   _mailQueue;

    public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IMailQueue mailQueue)
    {
        _userManager = userManager;
        _mailQueue   = mailQueue;
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
        // auch wenn kein Konto existiert oder die Mail bereits bestätigt ist
        // (verhindert User-Enumeration).
        if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            var token   = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmLink = $"{Request.Scheme}://{Request.Host}/Identity/Account/ConfirmEmail" +
                               $"?userId={user.Id}&code={encoded}";
            var fullName = $"{user.FirstName} {user.LastName}";

            _mailQueue.Enqueue(m => m.SendEmailConfirmationAsync(user.Email!, fullName, confirmLink));
        }

        EmailSent = true;
        return Page();
    }
}
