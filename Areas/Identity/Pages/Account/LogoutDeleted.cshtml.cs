using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FlomiApp.Data.Models;

namespace FlomiApp.Areas.Identity.Pages.Account
{
    public class LogoutDeletedModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoutDeletedModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}