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
    // AREA TEMPLATES (Stammdaten)
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<List<AreaTemplate>> GetAllAreaTemplatesAsync()
    {
        return await _context.AreaTemplates
            .Include(t => t.AreaCategory)
            .Include(t => t.Areas)
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<AreaTemplate?> GetAreaTemplateByIdAsync(int id)
    {
        return await _context.AreaTemplates
            .Include(t => t.AreaCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task CreateAreaTemplateAsync(AreaTemplate template)
    {
        _context.AreaTemplates.Add(template);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAreaTemplateAsync(AreaTemplate template)
    {
        var existing = await _context.AreaTemplates.FindAsync(template.Id);
        if (existing != null)
        {
            existing.Name           = template.Name;
            existing.MinAge         = template.MinAge;
            existing.Location       = template.Location;
            existing.AreaCategoryId = template.AreaCategoryId;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAreaTemplateAsync(int id)
    {
        var template = await _context.AreaTemplates.FindAsync(id);
        if (template != null)
        {
            _context.AreaTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AREAS (Zuweisungen)
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<int> CopyAreasToEventAsync(int sourceEventId, int targetEventId)
    {
        var targetEvent = await _context.Events.FindAsync(targetEventId);
        if (targetEvent == null) return 0;

        var sourceAreas = await _context.Areas
            .Where(a => a.EventId == sourceEventId)
            .AsNoTracking()
            .ToListAsync();

        foreach (var src in sourceAreas)
        {
            _context.Areas.Add(new Area
            {
                AreaTemplateId = src.AreaTemplateId,
                EventId        = targetEventId,
                Date           = targetEvent.Date,
                TimeSlot       = src.TimeSlot,
                MaxCapacity    = src.MaxCapacity,
            });
        }

        await _context.SaveChangesAsync();
        return sourceAreas.Count;
    }

    public async Task<List<Area>> GetAllAreasAsync()
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Include(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Area>> GetAreasWithAppointmentsAsync(int eventId)
    {
        return await _context.Areas
            .Include(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory)
            .Include(a => a.Appointments)
                .ThenInclude(ap => ap.User)
            .Include(a => a.Appointments)
                .ThenInclude(ap => ap.FamilyMember)
            .Where(a => a.EventId == eventId)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.AreaTemplate!.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Area>> GetAreasByEventAsync(int eventId)
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Include(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory)
            .Where(a => a.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Area> GetAreaByIdAsync(int id)
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Include(a => a.AreaTemplate).ThenInclude(t => t!.AreaCategory)
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
            existing.AreaTemplateId = area.AreaTemplateId;
            existing.MaxCapacity    = area.MaxCapacity;
            existing.Date           = area.Date;
            existing.TimeSlot       = area.TimeSlot;
            existing.EventId        = area.EventId;
            await _context.SaveChangesAsync();
        }
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
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.Events.Update(evt);
            await _context.SaveChangesAsync();
        }
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
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task MoveCategoryAsync(int id, int direction)
    {
        var all = await _context.AreaCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        var idx = all.FindIndex(c => c.Id == id);
        if (idx < 0) return;

        var swapIdx = idx + direction;
        if (swapIdx < 0 || swapIdx >= all.Count) return;

        (all[idx].SortOrder, all[swapIdx].SortOrder) = (all[swapIdx].SortOrder, all[idx].SortOrder);
        await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.AreaCategories.Update(category);
            await _context.SaveChangesAsync();
        }
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
