using System.Text;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace FlomiApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public bool Success { get; set; }

    public async Task OnGetAsync(string? userId, string? code)
    {
        if (userId == null || code == null)
        {
            Success = false;
            return;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            Success = false;
            return;
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            // Bereits bestätigt (z.B. Link zweimal angeklickt) — als Erfolg behandeln.
            Success = true;
            return;
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch
        {
            Success = false;
            return;
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        Success = result.Succeeded;
    }
}
