using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAreaService _areaService;

    public AppointmentService(ApplicationDbContext context, IAreaService areaService)
    {
        _context = context;
        _areaService = areaService;
    }

    public async Task<List<Appointment>> GetUserAppointmentsAsync(string userId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task RegisterForAppointmentAsync(string userId, int areaId)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null)
        {
            throw new InvalidOperationException("Area not found.");
        }

        if (!await CanRegisterAsync(userId, areaId))
        {
            var message = area.Category == AreaCategory.Verkauf
                ? "You can only register for one Verkauf area."
                : "Area is full.";
            throw new InvalidOperationException(message);
        }

        var appointment = new Appointment
        {
            UserId = userId,
            AreaId = areaId,
            Status = AppointmentStatus.Registered
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task CancelAppointmentAsync(int appointmentId, string userId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.UserId == userId);

        if (appointment != null)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> CanRegisterAsync(string userId, int areaId)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        if (area == null)
        {
            return false;
        }

        var current = await _areaService.GetCurrentRegistrationsAsync(areaId);
        if (current >= area.MaxCapacity)
        {
            return false;
        }

        if (area.Category == AreaCategory.Verkauf)
        {
            return !await UserHasRegisteredSaleAreaAsync(userId);
        }

        return true;
    }

    private async Task<bool> UserHasRegisteredSaleAreaAsync(string userId)
    {
        return await _context.Appointments
            .Include(a => a.Area)
            .Where(a => a.UserId == userId && a.Status == AppointmentStatus.Registered)
            .AnyAsync(a => a.Area.Category == AreaCategory.Verkauf);
    }
}