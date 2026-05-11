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
        if (!await CanRegisterAsync(areaId))
        {
            throw new InvalidOperationException("Area is full.");
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

    public async Task<bool> CanRegisterAsync(int areaId)
    {
        var area = await _areaService.GetAreaByIdAsync(areaId);
        var current = await _areaService.GetCurrentRegistrationsAsync(areaId);
        return current < area.MaxCapacity;
    }
}