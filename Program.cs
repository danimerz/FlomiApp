using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using FlomiApp.Areas.Identity;
using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Services;
using FlomiApp.Security;
using FlomiApp.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion), ServiceLifetime.Scoped);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.SignIn.RequireConfirmedEmail   = true;

            // Passwort-Policy
            options.Password.RequiredLength          = 6;
            options.Password.RequireDigit            = true;
            options.Password.RequireUppercase        = true;
            options.Password.RequireNonAlphanumeric  = true;

            // Brute-Force-Schutz: Konto nach 5 Fehlversuchen 10 Min sperren
            options.Lockout.MaxFailedAccessAttempts  = 5;
            options.Lockout.DefaultLockoutTimeSpan   = TimeSpan.FromMinutes(10);
            options.Lockout.AllowedForNewUsers        = true;
        })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

// Externe Logins (Google, Microsoft, MiData) – nur aktiv, wenn unter "ExternalAuth" konfiguriert
var externalLogins = builder.Services.AddExternalLogins(builder.Configuration);

// Hinter dem Reverse Proxy: https-Schema übernehmen, sonst stimmen die Redirect-URIs
// der externen Logins nicht (redirect_uri_mismatch). Vertraut wird standardmässig nur
// localhost; ein Proxy auf einer anderen IP gehört in "ForwardedHeaders:KnownProxies".
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        options.KnownProxies.Add(IPAddress.Parse(proxy));
});

builder.Services.AddAuthorization(options =>
{
    // Zusätzlich zur Rollen-Prüfung: Admin-Rollen müssen 2FA aktiviert haben.
    // Wer keine Admin-Rolle hat, erfüllt diese Policy trivial (betrifft nur
    // Seiten, die zusätzlich auch [Authorize(Roles = ...)] verlangen).
    options.AddPolicy(Roles.RequireTwoFactorForAdmins, policy =>
        policy.RequireAssertion(ctx =>
        {
            var isAdmin = Roles.AllAdminRoles.Any(ctx.User.IsInRole);
            return !isAdmin || ctx.User.HasClaim("tfa_enrolled", "true");
        }));
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IFurniturePickupService, FurniturePickupService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddSingleton<MailQueueService>();
builder.Services.AddSingleton<IMailQueue>(sp => sp.GetRequiredService<MailQueueService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MailQueueService>());
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddHostedService<PickupImageMaintenanceService>();

var app = builder.Build();

app.UseForwardedHeaders();

// form-action gilt auch für Weiterleitungen nach dem Absenden – der Login-Button
// leitet zum externen Anbieter weiter, dessen Host muss deshalb erlaubt sein.
var formActionSources = string.Join(" ", externalLogins.FormActionHosts.Prepend("'self'"));

// Sicherheits-Header für alle Antworten (CSP abgestimmt auf Chart.js/jsQR via jsdelivr,
// Blazor Server WebSocket-Verbindung und die vielen Inline-style-Attribute im Markup).
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.Append("X-Content-Type-Options", "nosniff");
    headers.Append("X-Frame-Options", "DENY");
    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.Append("Permissions-Policy", "camera=(self), microphone=(), geolocation=()");
    headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self' wss: ws:; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        $"form-action {formActionSources};");
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapPickupImages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Apply any pending EF Core migrations and seed admin user and role
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedAdminUserAsync(services);
}

app.Run();

async Task SeedAdminUserAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in new[] { "Admin", "FahrzeugAdmin", "MoebelAdmin", "BereichsAdmin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@flomiapp.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var config          = services.GetRequiredService<IConfiguration>();
        var configuredPw    = config["AdminPassword"];
        var isAutoGenerated = string.IsNullOrEmpty(configuredPw);
        var adminPassword   = configuredPw ?? GenerateSecurePassword();

        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            // Bei auto-generiertem Passwort: Login nur mit sofortiger Änderung.
            // Bei explizit konfiguriertem Passwort: Betreiber hat es bewusst gesetzt.
            MustChangePassword = isAutoGenerated
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");

            if (isAutoGenerated)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(
                    "\n" +
                    "══════════════════════════════════════════════════════════════\n" +
                    " Admin-Konto erstellt: {Email}\n" +
                    " Generiertes Passwort: {Password}\n" +
                    " Dieses Passwort wird NUR JETZT angezeigt. Beim ersten Login\n" +
                    " muss es geändert werden. Um ein eigenes Passwort zu setzen,\n" +
                    " vor dem ersten Start 'AdminPassword' konfigurieren.\n" +
                    "══════════════════════════════════════════════════════════════",
                    adminEmail, adminPassword);
            }
        }
    }
}

string GenerateSecurePassword()
{
    const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string lower   = "abcdefghjkmnpqrstuvwxyz";
    const string digits  = "23456789";
    const string special = "!#$%+-=?";
    const string all     = upper + lower + digits + special;

    var chars = new List<char>
    {
        PickRandom(upper),  PickRandom(upper),
        PickRandom(lower),  PickRandom(lower),
        PickRandom(digits), PickRandom(digits),
        PickRandom(special)
    };

    while (chars.Count < 16)
        chars.Add(PickRandom(all));

    // Fisher-Yates, kryptografisch sicher gemischt
    for (var i = chars.Count - 1; i > 0; i--)
    {
        var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
        (chars[i], chars[j]) = (chars[j], chars[i]);
    }

    return new string(chars.ToArray());

    char PickRandom(string set) => set[System.Security.Cryptography.RandomNumberGenerator.GetInt32(set.Length)];
}