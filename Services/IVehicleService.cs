using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IVehicleService
{
    // Stammdaten
    Task<List<Vehicle>>  GetAllVehiclesAsync();
    Task<Vehicle?>       GetVehicleByIdAsync(int id);
    Task                 CreateVehicleAsync(Vehicle vehicle);
    Task                 UpdateVehicleAsync(Vehicle vehicle);
    Task                 DeleteVehicleAsync(int id);

    // Zuweisungen
    Task<List<VehicleAssignment>> GetAssignmentsByEventAsync(int eventId);
    Task                          CreateAssignmentAsync(VehicleAssignment assignment, List<AssignmentDate> dates);
    Task                          UpdateAssignmentAsync(VehicleAssignment assignment, List<AssignmentDate> dates);
    Task                          DeleteAssignmentAsync(int id);

    // Einsatzplan (Fahrzeuge × Tage)
    Task                           SaveAssignmentDayAsync(int vehicleId, int eventId, AssignmentDate day);
    Task                           RemoveAssignmentDayAsync(int vehicleId, int eventId, DateTime date);
    Task<CrewAssignmentResult>     AutoAssignCrewAsync(int eventId, DateTime? date = null);
    Task<int>                      CopyAssignmentsFromEventAsync(int sourceEventId, int targetEventId);
    Task<List<VehicleDayCoverage>> GetDayCoverageAsync(IEnumerable<int> eventIds);

    // Users
    Task<List<ApplicationUser>>                         GetAllUsersAsync();
    Task<Dictionary<DateTime, List<ApplicationUser>>>   GetUsersByAreaNameAndEventAsync(int eventId, string areaTemplateName);
    Task<List<AssignmentDate>>                          GetVehicleAssignmentsForUserAsync(string userId);
    Task<Dictionary<DateTime, HashSet<string>>>         GetAlreadyAssignedUserIdsByDateAsync(int eventId, IEnumerable<int> excludeAssignmentIds);
}