using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FlomiApp.Data.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public IdentityUser User { get; set; } = null!;

    public int AreaId { get; set; }

    public Area Area { get; set; } = null!;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Registered;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum AppointmentStatus
{
    Registered,
    Cancelled
}