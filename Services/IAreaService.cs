using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IAreaService
{
    Task<List<Area>> GetAllAreasAsync();
    Task<List<Area>> GetAreasByEventAsync(int eventId);
    Task<Area> GetAreaByIdAsync(int id);
    Task CreateAreaAsync(Area area);
    Task UpdateAreaAsync(Area area);
    Task DeleteAreaAsync(int id);
    Task<int> GetCurrentRegistrationsAsync(int areaId);

    Task<List<Event>> GetAllEventsAsync();
    Task<Event> GetEventByIdAsync(int id);
    Task CreateEventAsync(Event evt);
    Task UpdateEventAsync(Event evt);
    Task DeleteEventAsync(int id);
}