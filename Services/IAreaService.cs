using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IAreaService
{
    // ── Area Templates (Stammdaten) ───────────────────────────────────────────
    Task<List<AreaTemplate>> GetAllAreaTemplatesAsync();
    Task<AreaTemplate?>      GetAreaTemplateByIdAsync(int id);
    Task                     CreateAreaTemplateAsync(AreaTemplate template);
    Task                     UpdateAreaTemplateAsync(AreaTemplate template);
    Task                     DeleteAreaTemplateAsync(int id);

    // ── Areas (Zuweisungen) ───────────────────────────────────────────────────
    Task<int>        CopyAreasToEventAsync(int sourceEventId, int targetEventId);
    Task<List<Area>> GetAllAreasAsync();
    Task<List<Area>> GetAreasWithAppointmentsAsync(int eventId);
    Task<List<Area>> GetAreasByEventAsync(int eventId);
    Task<Area>       GetAreaByIdAsync(int id);
    Task             CreateAreaAsync(Area area);
    Task             UpdateAreaAsync(Area area);
    Task             DeleteAreaAsync(int id);
    Task<int>        GetCurrentRegistrationsAsync(int areaId);

    // ── Events ────────────────────────────────────────────────────────────────
    Task<List<Event>> GetAllEventsAsync();
    Task<Event>       GetEventByIdAsync(int id);
    Task              CreateEventAsync(Event evt);
    Task              UpdateEventAsync(Event evt);
    Task              DeleteEventAsync(int id);

    // ── Categories ────────────────────────────────────────────────────────────
    Task<List<AreaCategory>> GetAllCategoriesAsync();
    Task<AreaCategory>       GetCategoryByIdAsync(int id);
    Task                     CreateCategoryAsync(AreaCategory category);
    Task                     UpdateCategoryAsync(AreaCategory category);
    Task                     DeleteCategoryAsync(int id);
    Task                     MoveCategoryAsync(int id, int direction); // -1 = up, +1 = down
}
