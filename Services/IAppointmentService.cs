using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IAppointmentService
{
    Task<List<Appointment>> GetUserAppointmentsAsync(string userId);
    Task RegisterForAppointmentAsync(string userId, int areaId);
    Task CancelAppointmentAsync(int appointmentId, string userId);
    Task<bool> CanRegisterAsync(string userId, int areaId);
}