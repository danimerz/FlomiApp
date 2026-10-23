using Microsoft.AspNetCore.Authentication;

namespace FlomiApp.Areas.Identity.Pages.Account;

/// <summary>Daten für das Partial _ExternalLoginButtons (Login- und Registrierungsseite).</summary>
public sealed record ExternalLoginButtonsModel(
    IList<AuthenticationScheme> Schemes,
    string? ReturnUrl,
    string DividerText,
    string? Hint = null);
