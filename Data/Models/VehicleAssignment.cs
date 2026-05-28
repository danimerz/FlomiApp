namespace FlomiApp.Data.Models;

public class VehicleAssignment
{
    public int    Id        { get; set; }
    public int    VehicleId { get; set; }
    public int    EventId   { get; set; }

    public Vehicle              Vehicle       { get; set; } = null!;
    public Event                Event         { get; set; } = null!;
    public List<AssignmentDate> AssignedDates { get; set; } = new();
}
