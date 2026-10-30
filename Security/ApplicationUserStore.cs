using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Security;

/// <summary>
/// Identity-UserStore mit Passkey-Unterstützung. Die Passkey-Logik (WebAuthn-Prüfung) kommt
/// aus ASP.NET Core Identity 10; nur das Speichern machen wir selbst, weil der passende
/// EF-Store (Identity.EntityFrameworkCore 10) EF Core 10 braucht und Pomelo noch auf 9 ist.
/// Sobald EF Core 10 da ist, kann diese Klasse entfallen (Daten aus <c>UserPasskeys</c> übernehmen).
/// </summary>
public class ApplicationUserStore
    : UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>, IUserPasskeyStore<ApplicationUser>
{
    public ApplicationUserStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
    }

    private DbSet<UserPasskey> Passkeys => Context.UserPasskeys;

    public async Task AddOrUpdatePasskeyAsync(ApplicationUser user, UserPasskeyInfo passkey, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(passkey);

        var entity = await Passkeys.SingleOrDefaultAsync(
            p => p.UserId == user.Id && p.CredentialId == passkey.CredentialId, cancellationToken);

        if (entity == null)
        {
            entity = new UserPasskey
            {
                UserId       = user.Id,
                CredentialId = passkey.CredentialId,
                CreatedAt    = passkey.CreatedAt.UtcDateTime,
            };
            Passkeys.Add(entity);
        }

        entity.PublicKey         = passkey.PublicKey;
        entity.Name              = passkey.Name;
        entity.SignCount         = passkey.SignCount;
        entity.Transports        = passkey.Transports is { Length: > 0 } t ? string.Join(",", t) : null;
        entity.IsUserVerified    = passkey.IsUserVerified;
        entity.IsBackupEligible  = passkey.IsBackupEligible;
        entity.IsBackedUp        = passkey.IsBackedUp;
        entity.AttestationObject = passkey.AttestationObject;
        entity.ClientDataJson    = passkey.ClientDataJson;

        await SaveChanges(cancellationToken);
    }

    public async Task<IList<UserPasskeyInfo>> GetPasskeysAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);

        var passkeys = await Passkeys.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return passkeys.Select(ToInfo).ToList();
    }

    public async Task<ApplicationUser?> FindByPasskeyIdAsync(byte[] credentialId, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(credentialId);

        var userId = await Passkeys
            .Where(p => p.CredentialId == credentialId)
            .Select(p => p.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        return userId == null ? null : await FindByIdAsync(userId, cancellationToken);
    }

    public async Task<UserPasskeyInfo?> FindPasskeyAsync(ApplicationUser user, byte[] credentialId, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(credentialId);

        var passkey = await Passkeys.AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == user.Id && p.CredentialId == credentialId, cancellationToken);
        return passkey == null ? null : ToInfo(passkey);
    }

    public async Task RemovePasskeyAsync(ApplicationUser user, byte[] credentialId, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(credentialId);

        var passkey = await Passkeys
            .SingleOrDefaultAsync(p => p.UserId == user.Id && p.CredentialId == credentialId, cancellationToken);
        if (passkey != null)
        {
            Passkeys.Remove(passkey);
            await SaveChanges(cancellationToken);
        }
    }

    private static UserPasskeyInfo ToInfo(UserPasskey p) =>
        new(p.CredentialId,
            p.PublicKey,
            new DateTimeOffset(DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc)),
            p.SignCount,
            string.IsNullOrEmpty(p.Transports) ? null : p.Transports.Split(','),
            p.IsUserVerified,
            p.IsBackupEligible,
            p.IsBackedUp,
            p.AttestationObject,
            p.ClientDataJson)
        {
            Name = p.Name,
        };
}
