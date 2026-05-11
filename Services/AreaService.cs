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
        return await _context.Areas.Include(a => a.Appointments).ToListAsync();
    }

    public async Task<Area> GetAreaByIdAsync(int id)
    {
        return await _context.Areas.Include(a => a.Appointments).FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task CreateAreaAsync(Area area)
    {
        _context.Areas.Add(area);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAreaAsync(Area area)
    {
        _context.Areas.Update(area);
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
}