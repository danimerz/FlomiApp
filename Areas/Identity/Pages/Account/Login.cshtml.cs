// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using FlomiApp.Data.Models;
using FlomiApp.Security;
using FlomiApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FlomiApp.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IPasskeyService _passkeyService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            IPasskeyService passkeyService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _passkeyService = passkeyService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "E-Mail")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Passwort")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Angemeldet bleiben")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Fehlversuche zählen für die Kontosperre (Lockout-Einstellungen in Program.cs)
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        if (user.MustChangePassword)
                        {
                            return RedirectToPage("./ChangePasswordRequired");
                        }

                        if (!user.TwoFactorEnabled)
                        {
                            var roles = await _userManager.GetRolesAsync(user);
                            if (roles.Any(Roles.AllAdminRoles.Contains))
                            {
                                return RedirectToPage("./TwoFactorSetup", new { ReturnUrl = returnUrl });
                            }
                        }
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty,
                        "Bitte bestätige zuerst deine E-Mail-Adresse (Link aus der Mail nach der Registrierung). " +
                        "Nichts erhalten? Fordere unten einen neuen Link an.");
                    return Page();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Anmeldung mit Passkey: <paramref name="credentialJson"/> ist die Antwort von
        /// navigator.credentials.get() (wwwroot/js/passkey.js). Ein Passkey zählt als zwei Faktoren –
        /// auch Admins kommen so ohne Authenticator-Code rein (amr-Claim «passkey», siehe Security/Passkeys.cs).
        /// </summary>
        public async Task<IActionResult> OnPostPasskeyAsync(string credentialJson, bool rememberMe, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Die E-Mail/Passwort-Felder gehören nicht zu diesem Formular
            ModelState.Clear();

            if (string.IsNullOrEmpty(credentialJson))
            {
                ModelState.AddModelError(string.Empty, "Die Anmeldung mit Passkey hat nicht geklappt. Bitte erneut versuchen.");
                return Page();
            }

            var assertion = await _signInManager.PerformPasskeyAssertionAsync(credentialJson);
            if (!assertion.Succeeded || !assertion.Passkey.IsUserVerified)
            {
                _logger.LogWarning("Passkey-Anmeldung fehlgeschlagen: {Message}",
                    assertion.Succeeded ? "ohne Benutzerprüfung" : assertion.Failure.Message);
                ModelState.AddModelError(string.Empty,
                    "Dieser Passkey ist nicht (mehr) bei der FlomiApp gespeichert. Melde dich mit E-Mail und Passwort an.");
                return Page();
            }

            var user = assertion.User;
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Passkey-Anmeldung für gesperrtes Konto.");
                return RedirectToPage("./Lockout");
            }
            if (!await _signInManager.CanSignInAsync(user))
            {
                ModelState.AddModelError(string.Empty,
                    "Bitte bestätige zuerst deine E-Mail-Adresse (Link aus der Mail nach der Registrierung).");
                return Page();
            }

            // Signaturzähler nachführen (Schutz vor geklonten Schlüsseln) und «zuletzt verwendet» merken
            await _userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);
            await _passkeyService.MarkUsedAsync(assertion.Passkey.CredentialId);

            await _signInManager.SignInWithClaimsAsync(user, rememberMe,
                new[] { new Claim(Passkeys.AmrClaimType, Passkeys.AuthenticationMethod) });
            _logger.LogInformation("User logged in with passkey.");

            if (user.MustChangePassword)
            {
                return RedirectToPage("./ChangePasswordRequired");
            }

            return LocalRedirect(returnUrl);
        }
    }
}
