namespace FlomiApp.Data.Models;

public class AssignmentDate
{
    public int      Id            { get; set; }
    public int      AssignmentId  { get; set; }
    public DateTime Date          { get; set; }

    public string?  DriverUserId  { get; set; }
    public string?  DriverName    { get; set; }
    public string?  DriverPhone   { get; set; }
    public string?  HelperUserId  { get; set; }
    public string?  HelperName    { get; set; }
    public string?  ReadyFrom     { get; set; }
    public string?  PickedUpBy    { get; set; }
    public string?  ReturnedBy    { get; set; }

    public VehicleAssignment Assignment { get; set; } = null!;
}
