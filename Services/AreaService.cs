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

    public async Task<List<Area>> GetAllAreasAsync()
    {
        return await _context.Areas.Include(a => a.Appointments).Include(a => a.Event).AsNoTracking().ToListAsync();
    }

    public async Task<List<Area>> GetAreasByEventAsync(int eventId)
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
            .Where(a => a.EventId == eventId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Area> GetAreaByIdAsync(int id)
    {
        return await _context.Areas
            .Include(a => a.Appointments)
            .Include(a => a.Event)
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
        var existingArea = await _context.Areas.FindAsync(area.Id);
        if (existingArea != null)
        {
            existingArea.Name = area.Name;
            existingArea.MaxCapacity = area.MaxCapacity;
            existingArea.Date = area.Date;
            existingArea.TimeSlot = area.TimeSlot;
            existingArea.Category = area.Category;
            existingArea.MinAge = area.MinAge;
            existingArea.EventId = area.EventId;
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

    public async Task<List<Event>> GetAllEventsAsync()
    {
        return await _context.Events.AsNoTracking().ToListAsync();
    }

    public async Task<Event> GetEventByIdAsync(int id)
    {
        return await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException($"Event with id {id} not found.");
    }

    public async Task CreateEventAsync(Event evt)
    {
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(Event evt)
    {
        var existingEvent = await _context.Events.FindAsync(evt.Id);
        if (existingEvent != null)
        {
            existingEvent.Name = evt.Name;
            existingEvent.Date = evt.Date;
            existingEvent.Description = evt.Description;
            existingEvent.IsActive = evt.IsActive;
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
}