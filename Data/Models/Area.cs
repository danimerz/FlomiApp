using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class Area
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int MaxCapacity { get; set; }

    public DateTime Date { get; set; }

    public string Location { get; set; } = string.Empty;

    public string TimeSlot { get; set; } = string.Empty; // e.g., "9:00-11:00"

    public int MinAge { get; set; }

    // ── Category FK (neu) ─────────────────────────────────
    public int          AreaCategoryId { get; set; }
    public AreaCategory? AreaCategory  { get; set; }

    // ── Event FK ──────────────────────────────────────────
    public int   EventId { get; set; }
    public Event Event   { get; set; } = null!;

    // ── Navigation ────────────────────────────────────────
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}