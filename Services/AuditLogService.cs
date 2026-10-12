using System.Security.Claims;
using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(ClaimsPrincipal admin, string action, string? targetDescription = null, string? details = null)
    {
        var adminId = admin.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var firstName = admin.FindFirstValue("FirstName");
        var lastName  = admin.FindFirstValue("LastName");
        var adminName = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)
            ? (admin.Identity?.Name ?? "Unbekannt")
            : $"{firstName} {lastName}".Trim();

        _context.AuditLogEntries.Add(new AuditLogEntry
        {
            Timestamp         = DateTime.UtcNow,
            AdminUserId       = adminId,
            AdminName         = adminName,
            Action            = action,
            TargetDescription = targetDescription,
            Details           = details
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLogEntry>> SearchAsync(
        string? adminFilter = null,
        string? actionFilter = null,
        DateTime? from = null,
        DateTime? to = null,
        int take = 300)
    {
        var query = _context.AuditLogEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(adminFilter))
            query = query.Where(e => e.AdminName.Contains(adminFilter));

        if (!string.IsNullOrWhiteSpace(actionFilter))
            query = query.Where(e => e.Action == actionFilter);

        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(take)
            .ToListAsync();
    }
}
