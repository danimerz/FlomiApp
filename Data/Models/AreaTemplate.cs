using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class AreaTemplate
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int    MinAge   { get; set; }
    public string Location { get; set; } = string.Empty;

    public int           AreaCategoryId { get; set; }
    public AreaCategory? AreaCategory   { get; set; }

    public List<Area> Areas { get; set; } = new();
}
