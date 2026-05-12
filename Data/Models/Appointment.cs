using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int AreaId { get; set; }

    public Area Area { get; set; } = null!;

    public int? FamilyMemberId { get; set; }
    public FamilyMember? FamilyMember { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Registered;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum AppointmentStatus
{
    Registered,
    Cancelled
}