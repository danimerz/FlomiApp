using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public enum AreaCategory
{
    Sammeln = 1,
    Sortieren = 2,
    Verkauf = 3
}

public class Area
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int MaxCapacity { get; set; }

    public DateTime Date { get; set; }

    public string TimeSlot { get; set; } = string.Empty; // e.g., "9:00-11:00"

    public AreaCategory Category { get; set; } = AreaCategory.Sammeln;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}