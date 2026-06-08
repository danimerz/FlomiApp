using Microsoft.AspNetCore.Components;
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
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail   = false;

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
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAddressSearchService, AddressSearchService>();

var app = builder.Build();

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
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var config        = services.GetRequiredService<IConfiguration>();
        var adminPassword = config["AdminPassword"] ?? "Admin123!";
        var result        = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}