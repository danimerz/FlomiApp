// Models/AreaCategory.cs
using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;
public class AreaCategory
{
    public int    Id   { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Area> Areas { get; set; } = new List<Area>();
}