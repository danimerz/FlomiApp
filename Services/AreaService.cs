using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AreaService : IAreaService
{
    private readonly ApplicationDbContext _context;

    public AreaService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AREAS
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<List<Area>> GetAllAreasAsync()
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Include(a => a.AreaCategory)   // ← neu
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Area>> GetAreasByEventAsync(int eventId)
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Include(a => a.AreaCategory)   // ← neu
            .Where(a => a.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Area> GetAreaByIdAsync(int id)
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Include(a => a.AreaCategory)   // ← neu
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new InvalidOperationException($"Area with id {id} not found.");
    }

    public async Task CreateAreaAsync(Area area)
    {
        _context.Areas.Add(area);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAreaAsync(Area area)
    {
        var existing = await _context.Areas.FindAsync(area.Id);
        if (existing != null)
        {
            existing.Name           = area.Name;
            existing.MaxCapacity    = area.MaxCapacity;
            existing.Date           = area.Date;
            existing.TimeSlot       = area.TimeSlot;
            existing.AreaCategoryId = area.AreaCategoryId;  // ← neu (war: Category)
            existing.MinAge         = area.MinAge;
            existing.EventId        = area.EventId;
            existing.Location       = area.Location;
        }
        else
        {
            _context.Areas.Update(area);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAreaAsync(int id)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area != null)
        {
            _context.Areas.Remove(area);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCurrentRegistrationsAsync(int areaId)
    {
        return await _context.Appointments
            .Where(a => a.AreaId == areaId && a.Status == AppointmentStatus.Registered)
            .CountAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EVENTS
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<List<Event>> GetAllEventsAsync()
    {
        return await _context.Events
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Event> GetEventByIdAsync(int id)
    {
        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Event with id {id} not found.");
    }

    public async Task CreateEventAsync(Event evt)
    {
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(Event evt)
    {
        var existing = await _context.Events.FindAsync(evt.Id);
        if (existing != null)
        {
            existing.Name        = evt.Name;
            existing.Date        = evt.Date;
            existing.Description = evt.Description;
            existing.IsActive    = evt.IsActive;
        }
        else
        {
            _context.Events.Update(evt);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteEventAsync(int id)
    {
        var evt = await _context.Events.FindAsync(id);
        if (evt != null)
        {
            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CATEGORIES
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<List<AreaCategory>> GetAllCategoriesAsync()
    {
        return await _context.AreaCategories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<AreaCategory> GetCategoryByIdAsync(int id)
    {
        return await _context.AreaCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException($"Category with id {id} not found.");
    }

    public async Task CreateCategoryAsync(AreaCategory category)
    {
        _context.AreaCategories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCategoryAsync(AreaCategory category)
    {
        var existing = await _context.AreaCategories.FindAsync(category.Id);
        if (existing != null)
        {
            existing.Name = category.Name;
        }
        else
        {
            _context.AreaCategories.Update(category);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.AreaCategories.FindAsync(id);
        if (category != null)
        {
            _context.AreaCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}