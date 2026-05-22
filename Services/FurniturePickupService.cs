// Services/FurniturePickupService.cs
using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class FurniturePickupService : IFurniturePickupService
{
    private readonly ApplicationDbContext _context;

    public FurniturePickupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FurniturePickupRequest> CreateRequestAsync(
        FurniturePickupRequest request,
        List<(byte[] Data, string ContentType, string FileName)> images)
    {
        // Auftragsnummer berechnen: höchste bestehende + 1, mindestens 1000
        var maxOrder = await _context.FurniturePickupRequests
            .MaxAsync(r => (int?)r.OrderNumber) ?? 999;
        request.OrderNumber = Math.Max(maxOrder + 1, 1000);

        // Bilder anhängen
        foreach (var (data, contentType, fileName) in images)
        {
            request.Images.Add(new FurniturePickupImage
            {
                ImageData   = data,
                ContentType = contentType,
                FileName    = fileName
            });
        }

        _context.FurniturePickupRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<List<FurniturePickupRequest>> GetUserRequestsAsync(string userId)
    {
        return await _context.FurniturePickupRequests
            .Include(r => r.Images)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FurniturePickupRequest>> GetAllRequestsAsync()
    {
        return await _context.FurniturePickupRequests
            .Include(r => r.User)
            .Include(r => r.Images)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<FurniturePickupRequest?> GetRequestByIdAsync(int id)
    {
        return await _context.FurniturePickupRequests
            .Include(r => r.User)
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task UpdateStatusAsync(int id, PickupRequestStatus status, string? adminNote)
    {
        var request = await _context.FurniturePickupRequests.FindAsync(id);
        if (request == null) return;

        request.Status    = status;
        request.AdminNote = adminNote;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRequestAsync(int id)
    {
        var request = await _context.FurniturePickupRequests
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request != null)
        {
            _context.FurniturePickupRequests.Remove(request);
            await _context.SaveChangesAsync();
        }
    }
    public async Task<FurniturePickupSettings> GetSettingsAsync()
    {
        var settings = await _context.FurniturePickupSettings
            .FirstOrDefaultAsync(s => s.Id == 1);

        // Falls noch gar kein Eintrag existiert → leeres Objekt zurückgeben
        return settings ?? new FurniturePickupSettings
        {
            Id             = 1,
            IsEnabled      = false,
            PickupDateFrom = null,
            PickupDateTo   = null
        };
    }

   public async Task SaveSettingsAsync(FurniturePickupSettings settings)
    {
        var existing = await _context.FurniturePickupSettings
            .FirstOrDefaultAsync(s => s.Id == 1);

        if (existing == null)
        {
            // Kein Eintrag vorhanden → neu anlegen
            var newSettings = new FurniturePickupSettings
            {
                Id             = 1,
                IsEnabled      = settings.IsEnabled,
                PickupDateFrom = settings.PickupDateFrom,
                PickupDateTo   = settings.PickupDateTo
            };
            await _context.FurniturePickupSettings.AddAsync(newSettings);
        }
        else
        {
            // Eintrag vorhanden → updaten
            existing.IsEnabled      = settings.IsEnabled;
            existing.PickupDateFrom = settings.PickupDateFrom;
            existing.PickupDateTo   = settings.PickupDateTo;
            _context.FurniturePickupSettings.Update(existing);
        }

        await _context.SaveChangesAsync();
    }
}