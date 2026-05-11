using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IAreaService
{
    Task<List<Area>> GetAllAreasAsync();
    Task<Area> GetAreaByIdAsync(int id);
    Task CreateAreaAsync(Area area);
    Task UpdateAreaAsync(Area area);
    Task DeleteAreaAsync(int id);
    Task<int> GetCurrentRegistrationsAsync(int areaId);
}