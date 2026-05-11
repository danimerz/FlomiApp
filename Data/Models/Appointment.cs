using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FlomiApp.Data.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; }

    public IdentityUser User { get; set; }

    public int AreaId { get; set; }

    public Area Area { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Registered;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum AppointmentStatus
{
    Registered,
    Cancelled
}