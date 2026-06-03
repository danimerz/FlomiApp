using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IAppointmentService
{
    Task<List<Appointment>> GetUserAppointmentsAsync(string userId);
    Task<List<FamilyMember>> GetFamilyMembersAsync(string userId);
    Task AddFamilyMemberAsync(string userId, FamilyMember familyMember);
    Task UpdateFamilyMemberAsync(int familyMemberId, string userId, FamilyMember familyMember);
    Task DeleteFamilyMemberAsync(int familyMemberId, string userId);
    Task RegisterForAppointmentAsync(string userId, int areaId, int? familyMemberId = null, string? comment = null, bool useAlternativeSlot = false);
    Task<Appointment?> GetByIdForCheckInAsync(int appointmentId);
    Task<bool>         CheckInAsync(int appointmentId);
    Task<bool>         CheckOutAsync(int appointmentId);
    Task CancelAppointmentAsync(int appointmentId, string userId);
    Task<bool> CanRegisterAsync(string userId, int areaId, int? familyMemberId = null, bool useAlternativeSlot = false);
}