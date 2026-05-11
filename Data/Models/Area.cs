using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class Area
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public int MaxCapacity { get; set; }

    public DateTime Date { get; set; }

    public string TimeSlot { get; set; } // e.g., "9:00-11:00"

    public ICollection<Appointment> Appointments { get; set; }
}