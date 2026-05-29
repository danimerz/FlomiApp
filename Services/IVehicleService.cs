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

    // Users
    Task<List<ApplicationUser>>                         GetAllUsersAsync();
    Task<Dictionary<DateTime, List<ApplicationUser>>>   GetUsersByAreaNameAndEventAsync(int eventId, string areaTemplateName);
}