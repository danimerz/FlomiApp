namespace FlomiApp.Data.Models;

public class Area
{
    public int      Id             { get; set; }
    public int      AreaTemplateId { get; set; }
    public int      EventId        { get; set; }
    public DateTime Date           { get; set; }
    public string   TimeSlot       { get; set; } = string.Empty;
    public int      MaxCapacity            { get; set; }
    public string?  AlternativeTimeSlot    { get; set; }
    public int?     AlternativeMaxCapacity { get; set; }

    public AreaTemplate? AreaTemplate { get; set; }
    public Event?        Event        { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
