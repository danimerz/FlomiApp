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
    /// <summary>
    /// «Ich komme spontan vorbei»: ohne fixes Datum und Zeit. <see cref="Date"/> bleibt der Eventtag
    /// (für Sortierung und Aufbewahrungsfristen), wird aber nicht angezeigt.
    /// </summary>
    public bool     IsSpontaneous          { get; set; }

    public AreaTemplate? AreaTemplate { get; set; }
    public Event?        Event        { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
