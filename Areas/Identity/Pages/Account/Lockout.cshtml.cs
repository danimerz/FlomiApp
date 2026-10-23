using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace FlomiApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LockoutModel : PageModel
{
    private readonly IdentityOptions _identityOptions;

    public LockoutModel(IOptions<IdentityOptions> identityOptions)
    {
        _identityOptions = identityOptions.Value;
    }

    // Sperrdauer aus der Lockout-Konfiguration in Program.cs
    public int LockoutMinutes => (int)Math.Ceiling(_identityOptions.Lockout.DefaultLockoutTimeSpan.TotalMinutes);

    public void OnGet() { }
}
