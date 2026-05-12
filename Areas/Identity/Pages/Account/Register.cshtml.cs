using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FlomiApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = string.Empty;

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "{0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string Pfadiname { get; set; } = string.Empty;

        public string Stufe { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string returnUrl = null!)
    {
        ReturnUrl = returnUrl ?? Url.Content("~");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null!)
    {
        returnUrl = returnUrl ?? Url.Content("~");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Pfadiname = Input.Pfadiname,
                Stufe = Input.Stufe,
                Birthday = Input.Birthday,
                PhoneNumber = Input.PhoneNumber
            };
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}
