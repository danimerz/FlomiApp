// Services/IFurniturePickupService.cs
using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IFurniturePickupService
{
    Task<FurniturePickupRequest> CreateRequestAsync(FurniturePickupRequest request, List<(byte[] Data, string ContentType, string FileName)> images);
    Task<List<FurniturePickupRequest>> GetUserRequestsAsync(string userId);
    Task<List<FurniturePickupRequest>> GetAllRequestsAsync();
    Task<FurniturePickupRequest?> GetRequestByIdAsync(int id);
    Task UpdateStatusAsync(int id, PickupRequestStatus status, string? adminNote);
    Task DeleteRequestAsync(int id);
    Task<FurniturePickupSettings> GetSettingsAsync();
    Task SaveSettingsAsync(FurniturePickupSettings settings);
}