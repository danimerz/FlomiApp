using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class Vehicle
{
       public int    Id           { get; set; }
    public string OwnerName    { get; set; } = "";
    public string? OwnerPhone  { get; set; }
    public string? OwnerContact { get; set; }

    /// <summary>Beispieldaten zum Testen der Routenplanung.</summary>
    public bool   IsSample     { get; set; }

    // Navigation
    public List<VehicleAssignment> Assignments { get; set; } = new();
}