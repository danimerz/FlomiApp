using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class AreaCategory
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public int    SortOrder { get; set; } = 999;

    public ICollection<AreaTemplate> AreaTemplates { get; set; } = new List<AreaTemplate>();
}
