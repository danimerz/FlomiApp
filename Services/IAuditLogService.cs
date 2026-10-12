using System.Security.Claims;
using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IAuditLogService
{
    Task LogAsync(ClaimsPrincipal admin, string action, string? targetDescription = null, string? details = null);

    Task<List<AuditLogEntry>> SearchAsync(
        string? adminFilter = null,
        string? actionFilter = null,
        DateTime? from = null,
        DateTime? to = null,
        int take = 300);
}
