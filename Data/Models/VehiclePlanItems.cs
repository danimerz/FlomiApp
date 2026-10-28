namespace FlomiApp.Data.Models;

/// <summary>Ergebnis von «Fahrer automatisch einteilen».</summary>
public record CrewAssignmentResult(int DriversAssigned, int HelpersAssigned);

/// <summary>Fahrzeuge und Möbelabholungen eines Tages – Grundlage für die Lücken-Hinweise.</summary>
public record VehicleDayCoverage(int EventId, DateTime Date, int Pickups, int Vehicles, int VehiclesWithDriver)
{
    public int  VehiclesWithoutDriver => Vehicles - VehiclesWithDriver;

    /// <summary>Abholungen geplant, aber kein Fahrzeug mit Fahrer.</summary>
    public bool PickupsWithoutCrew    => Pickups > 0 && VehiclesWithDriver == 0;

    public bool NeedsAttention        => PickupsWithoutCrew || VehiclesWithoutDriver > 0;
}

/// <summary>Bereiche, über die sich Fahrer und Beifahrer für einen Tag anmelden.</summary>
public static class VehicleCrewAreas
{
    public const string Driver = "Fahrer";
    public const string Helper = "Beifahrer";
}
