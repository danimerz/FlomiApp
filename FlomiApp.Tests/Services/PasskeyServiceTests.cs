using FlomiApp.Data.Models;
using FlomiApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlomiApp.Tests.Services;

public class PasskeyServiceTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    private PasskeyService CreateService() => new(_db.CreateContextFactory());

    private async Task<int> SeedAsync(string userId, byte credential, string name = "iPhone")
    {
        using var ctx = _db.CreateContext();
        if (await ctx.Users.FindAsync(userId) == null)
            ctx.Users.Add(new ApplicationUser
            {
                Id = userId, UserName = userId, Email = $"{userId}@example.com", FirstName = userId, LastName = userId,
            });
        var passkey = new UserPasskey
        {
            UserId = userId, CredentialId = new[] { credential }, Name = name,
            CreatedAt = new DateTime(2026, 10, credential, 0, 0, 0, DateTimeKind.Utc),
        };
        ctx.UserPasskeys.Add(passkey);
        await ctx.SaveChangesAsync();
        return passkey.Id;
    }

    [Fact]
    public async Task GetForUser_ReturnsOwnPasskeysOldestFirst_AndCounts()
    {
        await SeedAsync("dani", 2, "Mac");
        await SeedAsync("dani", 1, "iPhone");
        await SeedAsync("other", 3);
        var service = CreateService();

        Assert.Equal(new[] { "iPhone", "Mac" }, (await service.GetForUserAsync("dani")).Select(p => p.Name));
        Assert.Equal(2, await service.CountForUserAsync("dani"));

        var counts = await service.CountByUserAsync();
        Assert.Equal(2, counts["dani"]);
        Assert.Equal(1, counts["other"]);
    }

    [Fact]
    public async Task Rename_OnlyOwnPasskey_AndNormalizesName()
    {
        var id      = await SeedAsync("dani", 1);
        var service = CreateService();

        Assert.False(await service.RenameAsync("other", id, "Fremd"));
        Assert.True(await service.RenameAsync("dani", id, "  Arbeits-Laptop "));
        Assert.Equal("Arbeits-Laptop", (await service.GetForUserAsync("dani"))[0].Name);

        Assert.True(await service.RenameAsync("dani", id, ""));
        Assert.Equal("Passkey", (await service.GetForUserAsync("dani"))[0].Name);
    }

    [Fact]
    public async Task Delete_OnlyOwnPasskey()
    {
        var id      = await SeedAsync("dani", 1, "iPhone");
        var service = CreateService();

        Assert.Null(await service.DeleteAsync("other", id));
        Assert.Equal("iPhone", (await service.DeleteAsync("dani", id))?.Name);
        Assert.Equal(0, await service.CountForUserAsync("dani"));
    }

    [Fact]
    public async Task DeleteAll_RemovesOnlyThatUsersPasskeys()
    {
        await SeedAsync("dani", 1);
        await SeedAsync("dani", 2);
        await SeedAsync("other", 3);
        var service = CreateService();

        Assert.Equal(2, await service.DeleteAllAsync("dani"));
        Assert.Equal(0, await service.CountForUserAsync("dani"));
        Assert.Equal(1, await service.CountForUserAsync("other"));
    }

    [Fact]
    public async Task MarkUsed_SetsLastUsed()
    {
        await SeedAsync("dani", 1);
        var service = CreateService();

        await service.MarkUsedAsync(new byte[] { 1 });

        using var ctx = _db.CreateContext();
        var passkey = await ctx.UserPasskeys.SingleAsync();
        Assert.NotNull(passkey.LastUsedAt);
        Assert.True(DateTime.UtcNow - passkey.LastUsedAt!.Value < TimeSpan.FromMinutes(1));
    }
}
