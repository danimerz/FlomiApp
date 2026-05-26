namespace FlomiApp.Data.Models;

public class AppointmentSummaryItem
{
    public string   EventName { get; set; } = string.Empty;
    public string   AreaName  { get; set; } = string.Empty;
    public DateTime Date      { get; set; }
    public string   TimeSlot  { get; set; } = string.Empty;
    public string   Category  { get; set; } = string.Empty;
    public string?  ForPerson { get; set; }
}