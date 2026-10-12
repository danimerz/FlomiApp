using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlomiApp.Tests;

/// <summary>
/// Baut einen echten (nicht gemockten) UserManager direkt auf einem
/// ApplicationDbContext auf, ohne vollen DI-Container — Services, die einen
/// UserManager im Konstruktor verlangen (z.B. VehicleService), brauchen dafür
/// keine komplette Identity-Registrierung in Tests.
/// </summary>
public static class TestUserManager
{
    public static UserManager<ApplicationUser> Create(ApplicationDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>(context);

        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new List<IUserValidator<ApplicationUser>>(),
            new List<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
