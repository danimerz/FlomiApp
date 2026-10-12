namespace FlomiApp.Data.Models;

public class AuditLogEntry
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string AdminUserId { get; set; } = string.Empty;
    public string AdminName   { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    // Betroffene Person/Objekt, z.B. "Max Muster (max@example.com)"
    public string? TargetDescription { get; set; }

    // Zusätzlicher Freitext, z.B. "Rolle 'Admin' entfernt"
    public string? Details { get; set; }
}
