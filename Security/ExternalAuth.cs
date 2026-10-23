using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FlomiApp.Security;

/// <summary>
/// Konfiguration eines externen Login-Anbieters (Abschnitt "ExternalAuth:&lt;Anbieter&gt;").
/// Ein Anbieter wird nur registriert, wenn ClientId und ClientSecret gesetzt sind –
/// ohne Konfiguration bleibt die App beim reinen Passwort-Login.
/// </summary>
public class ExternalProviderOptions
{
    public string? ClientId     { get; set; }
    public string? ClientSecret { get; set; }

    // Darf die vom Anbieter gelieferte E-Mail als bestätigt gelten? Nur dann wird ein
    // bestehendes Konto automatisch verknüpft bzw. ein neues Konto ohne Bestätigungsmail
    // freigeschaltet. Liefert der Anbieter zusätzlich "email_verified", muss das true sein.
    public bool TrustEmail { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public class MiDataOptions : ExternalProviderOptions
{
    // Produktion: https://db.scout.ch · Integration/Test: https://pbs.puzzle.ch
    public string Authority { get; set; } = "https://db.scout.ch";
}

/// <summary>
/// Laufzeit-Infos zu den registrierten Anbietern (für Callback-Logik und CSP).
/// </summary>
public class ExternalLoginSettings
{
    private readonly Dictionary<string, bool> _trustEmail = new(StringComparer.OrdinalIgnoreCase);

    // Hosts, auf die der Login-POST weiterleitet – müssen in CSP form-action erlaubt sein
    public List<string> FormActionHosts { get; } = new();

    public void Add(string scheme, bool trustEmail, params string[] hosts)
    {
        _trustEmail[scheme] = trustEmail;
        FormActionHosts.AddRange(hosts);
    }

    public bool IsEmailTrusted(string scheme) =>
        _trustEmail.TryGetValue(scheme, out var trusted) && trusted;
}

public static class ExternalAuthSetup
{
    public const string GoogleScheme    = "Google";
    public const string MicrosoftScheme = "Microsoft";
    public const string MiDataScheme    = "MiData";

    public static ExternalLoginSettings AddExternalLogins(
        this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new ExternalLoginSettings();
        var auth     = services.AddAuthentication();

        var google = configuration.GetSection("ExternalAuth:Google").Get<ExternalProviderOptions>();
        if (google?.IsConfigured == true)
        {
            auth.AddGoogle(GoogleScheme, "Google", o =>
            {
                o.ClientId     = google.ClientId!;
                o.ClientSecret = google.ClientSecret!;
                o.ClaimActions.MapJsonKey(ExternalLoginProfile.EmailVerifiedClaim, "email_verified");
                o.Events.OnRemoteFailure = HandleRemoteFailure;
            });
            settings.Add(GoogleScheme, google.TrustEmail, "https://accounts.google.com");
        }

        var microsoft = configuration.GetSection("ExternalAuth:Microsoft").Get<ExternalProviderOptions>();
        if (microsoft?.IsConfigured == true)
        {
            auth.AddMicrosoftAccount(MicrosoftScheme, "Microsoft", o =>
            {
                o.ClientId     = microsoft.ClientId!;
                o.ClientSecret = microsoft.ClientSecret!;
                o.Events.OnRemoteFailure = HandleRemoteFailure;
            });
            // Private Microsoft-Konten werden über login.live.com weitergeleitet
            settings.Add(MicrosoftScheme, microsoft.TrustEmail,
                "https://login.microsoftonline.com", "https://login.live.com");
        }

        var midata = configuration.GetSection("ExternalAuth:MiData").Get<MiDataOptions>();
        if (midata?.IsConfigured == true)
        {
            auth.AddOpenIdConnect(MiDataScheme, "MiData", o =>
            {
                o.Authority    = midata.Authority;
                o.ClientId     = midata.ClientId;
                o.ClientSecret = midata.ClientSecret;
                o.ResponseType = OpenIdConnectResponseType.Code;
                o.CallbackPath = "/signin-midata";

                // MiData unterstützt PKCE nur mit "plain", der Handler nutzt S256 –
                // als vertraulicher Client mit Secret ist PKCE hier nicht nötig.
                o.UsePkce = false;

                o.Scope.Clear();
                o.Scope.Add("openid");
                o.Scope.Add("email");
                o.Scope.Add("name");
                o.Scope.Add("with_roles");

                // Pfadiname und Geburtstag kommen nur über den Userinfo-Endpoint
                o.GetClaimsFromUserInfoEndpoint = true;
                o.ClaimActions.MapUniqueJsonKey("first_name", "first_name");
                o.ClaimActions.MapUniqueJsonKey("last_name",  "last_name");
                o.ClaimActions.MapUniqueJsonKey("nickname",   "nickname");
                o.ClaimActions.MapUniqueJsonKey("birthday",   "birthday");
                o.Events.OnRemoteFailure = HandleRemoteFailure;
            });
            settings.Add(MiDataScheme, midata.TrustEmail, midata.Authority.TrimEnd('/'));
        }

        services.AddSingleton(settings);
        return settings;
    }

    // Fehler beim Anbieter oder beim Einlösen des Codes (z.B. falsches Client-Secret):
    // statt der rohen Fehlerseite zurück zum Login mit verständlicher Meldung,
    // der genaue Grund landet im Log.
    private static Task HandleRemoteFailure(RemoteFailureContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FlomiApp.ExternalAuth");
        logger.LogError(context.Failure, "Externer Login über {Scheme} fehlgeschlagen", context.Scheme.Name);

        // access_denied = Person hat beim Anbieter selbst abgebrochen
        var reason = context.Failure?.Message.Contains("access_denied", StringComparison.OrdinalIgnoreCase) == true
            ? "cancelled"
            : "failed";

        // RedirectUri ist unser Callback inkl. returnUrl – nur lokale Pfade zulassen
        var target = context.Properties?.RedirectUri;
        if (string.IsNullOrEmpty(target) || !target.StartsWith('/') || target.StartsWith("//"))
            target = "/Identity/Account/ExternalLogin?handler=Callback";

        var separator = target.Contains('?') ? '&' : '?';
        context.Response.Redirect($"{target}{separator}remoteError={reason}");
        context.HandleResponse();
        return Task.CompletedTask;
    }
}
