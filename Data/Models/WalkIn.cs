namespace FlomiApp.Data.Models;

public class WalkIn
{
    public int      Id           { get; set; }
    public int      EventId      { get; set; }
    public string   FirstName    { get; set; } = string.Empty;
    public string   LastName     { get; set; } = string.Empty;
    public string?  Info         { get; set; }
    public DateTime CheckedInAt  { get; set; } = DateTime.Now;
    public DateTime? CheckedOutAt { get; set; }

    public Event Event { get; set; } = null!;
}
