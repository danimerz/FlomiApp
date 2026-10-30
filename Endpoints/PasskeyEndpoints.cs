// Endpoints/PasskeyEndpoints.cs
using FlomiApp.Data.Models;
using FlomiApp.Security;
using FlomiApp.Services;
using Microsoft.AspNetCore.Identity;

namespace FlomiApp.Endpoints;

/// <summary>
/// Passkey-Abläufe, die den HttpContext brauchen (Identity merkt sich die Challenge in einem Cookie)
/// und deshalb nicht in der Blazor-Verbindung laufen können. Aufgerufen von <c>wwwroot/js/passkey.js</c>.
/// Die Anmeldung selbst läuft über die Login-Seite (Handler «Passkey»).
/// POST mit JSON-Body: fremde Seiten können das nicht ohne CORS-Freigabe auslösen; die
/// Challenge-Prüfung von WebAuthn schützt zusätzlich.
/// </summary>
public static class PasskeyEndpoints
{
    public record RegistrationRequest(string Credential, string? Name);

    public static IEndpointRouteBuilder MapPasskeys(this IEndpointRouteBuilder app)
    {
        // Schritt 1 beim Hinzufügen: Optionen für navigator.credentials.create()
        app.MapPost("/passkeys/creation-options", async (
                HttpContext http,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IPasskeyService passkeyService) =>
            {
                var user = await userManager.GetUserAsync(http.User);
                if (user == null) return Results.Unauthorized();

                if (await passkeyService.CountForUserAsync(user.Id) >= Passkeys.MaxPerUser)
                    return Results.BadRequest(new { error = $"Du hast bereits {Passkeys.MaxPerUser} Passkeys. Lösche zuerst einen alten." });

                var email       = user.Email ?? user.UserName ?? user.Id;
                var displayName = $"{user.FirstName} {user.LastName}".Trim();
                var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
                {
                    Id          = user.Id,
                    Name        = email,
                    DisplayName = displayName.Length > 0 ? displayName : email,
                });
                return Results.Content(optionsJson, "application/json");
            })
            .RequireAuthorization();

        // Schritt 2 beim Hinzufügen: Antwort des Geräts prüfen und speichern
        app.MapPost("/passkeys/register", async (
                RegistrationRequest request,
                HttpContext http,
                UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager,
                IAuditLogService auditLog,
                ILoggerFactory loggerFactory) =>
            {
                var user = await userManager.GetUserAsync(http.User);
                if (user == null) return Results.Unauthorized();

                var result = await signInManager.PerformPasskeyAttestationAsync(request.Credential);
                if (!result.Succeeded)
                {
                    loggerFactory.CreateLogger("FlomiApp.Passkeys").LogWarning("Passkey-Registrierung fehlgeschlagen: {Message}", result.Failure.Message);
                    return Results.BadRequest(new { error = "Der Passkey konnte nicht geprüft werden. Bitte erneut versuchen." });
                }

                // Die Optionen aus Schritt 1 gehören zu diesem Konto – sonst abbrechen
                if (result.UserEntity.Id != user.Id)
                    return Results.BadRequest(new { error = "Der Passkey gehört nicht zu diesem Konto." });

                if (await userManager.FindByPasskeyIdAsync(result.Passkey.CredentialId) != null)
                    return Results.BadRequest(new { error = "Dieser Passkey ist bereits gespeichert." });

                var passkey  = result.Passkey;
                passkey.Name = Passkeys.NormalizeName(request.Name);

                var saved = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
                if (!saved.Succeeded)
                    return Results.BadRequest(new { error = "Der Passkey konnte nicht gespeichert werden." });

                await auditLog.LogAsync(http.User, AuditActions.PasskeyAdded,
                    $"{user.FirstName} {user.LastName} ({user.Email})", passkey.Name);
                return Results.Ok(new { name = passkey.Name });
            })
            .RequireAuthorization();

        // Anmeldung: Optionen für navigator.credentials.get() – ohne Benutzer, das Gerät
        // schlägt die passenden Passkeys selbst vor (auffindbare Passkeys).
        app.MapPost("/passkeys/request-options", async (SignInManager<ApplicationUser> signInManager) =>
                Results.Content(await signInManager.MakePasskeyRequestOptionsAsync(user: null), "application/json"))
            .AllowAnonymous();

        return app;
    }
}
