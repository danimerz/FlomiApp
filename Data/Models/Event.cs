using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
