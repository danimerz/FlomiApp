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

    // ════════════════════════════════════════════════════════════════════════
    // REQUESTS
    // ════════════════════════════════════════════════════════════════════════

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
            .Include(r => r.Event)
            .Where(r => r.UserId == userId && r.Status != PickupRequestStatus.Deleted)
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
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request != null)
        {
            request.Status = PickupRequestStatus.Deleted;
            await _context.SaveChangesAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // SETTINGS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Globale Fallback-Settings (Id = 1) – für Rückwärtskompatibilität.
    /// </summary>
    public async Task<FurniturePickupSettings> GetSettingsAsync()
    {
        var settings = await _context.FurniturePickupSettings
            .FirstOrDefaultAsync(s => s.Id == 1);

        return settings ?? new FurniturePickupSettings
        {
            Id             = 1,
            IsEnabled      = false,
            PickupDateFrom = null,
            PickupDateTo   = null,
            EventId        = null
        };
    }

    /// <summary>
    /// Alle Settings-Einträge – eine pro Event.
    /// Gibt immer mindestens den globalen Fallback-Eintrag zurück.
    /// </summary>
    public async Task<List<FurniturePickupSettings>> GetAllSettingsAsync()
    {
        var list = await _context.FurniturePickupSettings
            .Include(s => s.Event)
            .OrderBy(s => s.Id)
            .ToListAsync();

        // Sicherheitsnetz: Falls DB leer ist, Fallback zurückgeben
        if (!list.Any())
        {
            list.Add(new FurniturePickupSettings
            {
                Id             = 1,
                IsEnabled      = false,
                PickupDateFrom = null,
                PickupDateTo   = null,
                EventId        = null
            });
        }

        return list;
    }

    /// <summary>
    /// Upsert-Logik:
    /// – Id == 0  → neuer Eintrag wird angelegt
    /// – Id  > 0  → bestehender Eintrag wird aktualisiert
    /// </summary>
    public async Task SaveSettingsAsync(FurniturePickupSettings settings)
    {
        if (settings.Id == 0)
        {
            // ── Neu anlegen ────────────────────────────────────────────────
            var newEntry = new FurniturePickupSettings
            {
                IsEnabled      = settings.IsEnabled,
                PickupDateFrom = settings.PickupDateFrom,
                PickupDateTo   = settings.PickupDateTo,
                EventId        = settings.EventId
            };
            await _context.FurniturePickupSettings.AddAsync(newEntry);
        }
        else
        {
            // ── Bestehenden updaten ────────────────────────────────────────
            var existing = await _context.FurniturePickupSettings
                .FirstOrDefaultAsync(s => s.Id == settings.Id);

            if (existing == null)
            {
                // Fallback: Falls Id nicht gefunden → neu anlegen
                var newEntry = new FurniturePickupSettings
                {
                    IsEnabled      = settings.IsEnabled,
                    PickupDateFrom = settings.PickupDateFrom,
                    PickupDateTo   = settings.PickupDateTo,
                    EventId        = settings.EventId
                };
                await _context.FurniturePickupSettings.AddAsync(newEntry);
            }
            else
            {
                existing.IsEnabled      = settings.IsEnabled;
                existing.PickupDateFrom = settings.PickupDateFrom;
                existing.PickupDateTo   = settings.PickupDateTo;
                existing.EventId        = settings.EventId;

                _context.FurniturePickupSettings.Update(existing);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Löscht einen Settings-Eintrag anhand der Id.
    /// Den globalen Fallback (Id = 1) schützen wir vor dem Löschen.
    /// </summary>
    public async Task DeleteSettingsAsync(int id)
    {
        // Globalen Fallback nicht löschen – nur deaktivieren
        if (id == 1)
        {
            var fallback = await _context.FurniturePickupSettings
                .FirstOrDefaultAsync(s => s.Id == 1);

            if (fallback != null)
            {
                fallback.IsEnabled = false;
                fallback.EventId   = null;
                await _context.SaveChangesAsync();
            }
            return;
        }

        var entry = await _context.FurniturePickupSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (entry != null)
        {
            _context.FurniturePickupSettings.Remove(entry);
            await _context.SaveChangesAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // EVENTS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Alle aktiven Events – für Dropdowns und Auswahl.
    /// </summary>
    public async Task<List<Event>> GetAvailableEventsAsync()
    {
        return await _context.Events
            .Where(e => e.IsActive)
            .OrderBy(e => e.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Gibt das aktive Settings-Objekt für einen bestimmten Event zurück.
    /// Null wenn kein aktives Setting für diesen Event existiert.
    /// </summary>
    public async Task<FurniturePickupSettings?> GetSettingsByEventIdAsync(int eventId)
    {
        return await _context.FurniturePickupSettings
            .Include(s => s.Event)
            .FirstOrDefaultAsync(s => s.EventId == eventId && s.IsEnabled);
    }

    public async Task<int> GetPickupCountForDateAsync(int eventId, DateTime date)
    {
        return await _context.FurniturePickupRequests
            .Where(r => r.EventId == eventId
                     && r.PickupDate.Date == date.Date
                     && (r.Status == PickupRequestStatus.Pending
                      || r.Status == PickupRequestStatus.Accepted))
            .CountAsync();
    }
}