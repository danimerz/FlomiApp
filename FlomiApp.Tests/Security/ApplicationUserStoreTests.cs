using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Tests.Security;

/// <summary>
/// Passkey-Speicher des eigenen UserStores (Tabelle UserPasskeys), angesprochen über den
/// UserManager – genau so ruft ihn der SignInManager von Identity 10 auf.
/// </summary>
public class ApplicationUserStoreTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private static UserPasskeyInfo Passkey(byte id, uint signCount = 0, string? name = "iPhone") =>
        new(new[] { id, id, id }, new byte[] { 9, 9 }, new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero),
            signCount, new[] { "internal", "hybrid" }, isUserVerified: true, isBackupEligible: true, isBackedUp: true,
            new byte[] { 1 }, new byte[] { 2 })
        {
            Name = name,
        };

    private static async Task<ApplicationUser> AddUserAsync(ApplicationDbContext ctx, string id)
    {
        var user = new ApplicationUser
        {
            Id = id, UserName = id, Email = $"{id}@example.com", FirstName = id, LastName = id,
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user;
    }

    [Fact]
    public void UserManager_SupportsPasskeys()
    {
        using var ctx = _db.CreateContext();
        Assert.True(TestUserManager.Create(ctx).SupportsUserPasskey);
    }

    [Fact]
    public async Task AddPasskey_RoundTripsAllFields_AndFindsUser()
    {
        using var ctx = _db.CreateContext();
        var users = TestUserManager.Create(ctx);
        var user  = await AddUserAsync(ctx, "dani");

        Assert.True((await users.AddOrUpdatePasskeyAsync(user, Passkey(7))).Succeeded);

        var stored = Assert.Single(await users.GetPasskeysAsync(user));
        Assert.Equal(new byte[] { 7, 7, 7 }, stored.CredentialId);
        Assert.Equal(new byte[] { 9, 9 }, stored.PublicKey);
        Assert.Equal("iPhone", stored.Name);
        Assert.Equal(new[] { "internal", "hybrid" }, stored.Transports);
        Assert.True(stored.IsUserVerified && stored.IsBackupEligible && stored.IsBackedUp);
        Assert.Equal(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero), stored.CreatedAt);

        Assert.Equal("dani", (await users.FindByPasskeyIdAsync(new byte[] { 7, 7, 7 }))?.Id);
        Assert.Null(await users.FindByPasskeyIdAsync(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task AddOrUpdate_SameCredential_UpdatesSignCountInsteadOfDuplicating()
    {
        using var ctx = _db.CreateContext();
        var users = TestUserManager.Create(ctx);
        var user  = await AddUserAsync(ctx, "dani");

        await users.AddOrUpdatePasskeyAsync(user, Passkey(7, signCount: 1));
        await users.AddOrUpdatePasskeyAsync(user, Passkey(7, signCount: 5, name: "Neuer Name"));

        var stored = Assert.Single(await users.GetPasskeysAsync(user));
        Assert.Equal(5u, stored.SignCount);
        Assert.Equal("Neuer Name", stored.Name);
    }

    [Fact]
    public async Task FindAndRemove_OnlyAffectOwnPasskeys()
    {
        using var ctx = _db.CreateContext();
        var users = TestUserManager.Create(ctx);
        var dani  = await AddUserAsync(ctx, "dani");
        var other = await AddUserAsync(ctx, "other");
        await users.AddOrUpdatePasskeyAsync(dani, Passkey(7));

        Assert.Null(await users.GetPasskeyAsync(other, new byte[] { 7, 7, 7 }));
        await users.RemovePasskeyAsync(other, new byte[] { 7, 7, 7 });
        Assert.Single(await users.GetPasskeysAsync(dani));

        Assert.True((await users.RemovePasskeyAsync(dani, new byte[] { 7, 7, 7 })).Succeeded);
        Assert.Empty(await users.GetPasskeysAsync(dani));
    }

    [Fact]
    public async Task DeletingUser_RemovesPasskeys()
    {
        using var ctx = _db.CreateContext();
        var users = TestUserManager.Create(ctx);
        var user  = await AddUserAsync(ctx, "dani");
        await users.AddOrUpdatePasskeyAsync(user, Passkey(7));

        await users.DeleteAsync(user);

        using var check = _db.CreateContext();
        Assert.Empty(await check.UserPasskeys.ToListAsync());
    }
}
