namespace FlomiApp.Data.Models;

public class ChatMessage
{
    public int      Id          { get; set; }
    public string   FromUserId  { get; set; } = string.Empty;
    public string   ToUserId    { get; set; } = string.Empty;
    public string   Body        { get; set; } = string.Empty;
    public DateTime SentAt      { get; set; } = DateTime.Now;
    public bool     IsRead      { get; set; } = false;
    public bool     IsFromAdmin { get; set; } = false;

    public ApplicationUser From { get; set; } = null!;
    public ApplicationUser To   { get; set; } = null!;
}
