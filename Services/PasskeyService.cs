using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Security;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class PasskeyService : IPasskeyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public PasskeyService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<UserPasskey>> GetForUserAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserPasskeys.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> CountForUserAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserPasskeys.CountAsync(p => p.UserId == userId);
    }

    public async Task<Dictionary<string, int>> CountByUserAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserPasskeys
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);
    }

    public async Task<bool> RenameAsync(string userId, int passkeyId, string? name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var passkey = await db.UserPasskeys.SingleOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);
        if (passkey == null) return false;

        passkey.Name = Passkeys.NormalizeName(name);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<UserPasskey?> DeleteAsync(string userId, int passkeyId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var passkey = await db.UserPasskeys.SingleOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);
        if (passkey == null) return null;

        db.UserPasskeys.Remove(passkey);
        await db.SaveChangesAsync();
        return passkey;
    }

    public async Task<int> DeleteAllAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var passkeys = await db.UserPasskeys.Where(p => p.UserId == userId).ToListAsync();
        db.UserPasskeys.RemoveRange(passkeys);
        await db.SaveChangesAsync();
        return passkeys.Count;
    }

    public async Task MarkUsedAsync(byte[] credentialId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var passkey = await db.UserPasskeys.SingleOrDefaultAsync(p => p.CredentialId == credentialId);
        if (passkey == null) return;

        passkey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
