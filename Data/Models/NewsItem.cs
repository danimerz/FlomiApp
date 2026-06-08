using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class NewsItem
{
    public int    Id          { get; set; }

    [Required]
    public string Title       { get; set; } = string.Empty;

    [Required]
    public string Content     { get; set; } = string.Empty;

    public DateTime CreatedAt   { get; set; } = DateTime.Now;
    public bool     IsPublished { get; set; } = true;
    public bool     IsPinned    { get; set; } = false;
}
