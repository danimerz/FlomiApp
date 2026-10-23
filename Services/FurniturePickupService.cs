// Services/FurniturePickupService.cs
using FlomiApp.Data;
using FlomiApp.Data.Models;
using FlomiApp.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public class FurniturePickupService : IFurniturePickupService
{
    /// <summary>Fotos werden so viele Monate nach dem Event gelöscht (siehe Datenschutzerklärung).</summary>
    public const int PhotoRetentionMonths = 3;

    private readonly ApplicationDbContext _context;
    private readonly IMailQueue           _mailQueue;
    private readonly ILogger<FurniturePickupService> _logger;

    public FurniturePickupService(ApplicationDbContext context,
        IMailQueue mailQueue, ILogger<FurniturePickupService> logger)
    {
        _context   = context;
        _mailQueue = mailQueue;
        _logger    = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // REQUESTS
    // ════════════════════════════════════════════════════════════════════════

    public async Task<FurniturePickupRequest> CreateRequestAsync(
        FurniturePickupRequest request,
        List<(ProcessedImage Image, string FileName)> images)
    {
        // Auftragsnummer berechnen: höchste bestehende + 1, mindestens 1000
        var maxOrder = await _context.FurniturePickupRequests
            .MaxAsync(r => (int?)r.OrderNumber) ?? 999;
        request.OrderNumber = Math.Max(maxOrder + 1, 1000);

        // Bilder anhängen
        foreach (var (image, fileName) in images)
            request.Images.Add(NewImage(image, fileName, caption: null));

        _context.FurniturePickupRequests.Add(request);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); // verhindert dass dasselbe Objekt beim nächsten Submit neu getrackt wird
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
            .Include(r => r.AssignedVehicle)
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

        SendStatusMailAsync(request);
    }

    public async Task DeleteRequestAsync(int id)
    {
        var request = await _context.FurniturePickupRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request != null)
        {
            request.Status = PickupRequestStatus.Deleted;
            await _context.SaveChangesAsync();

            SendStatusMailAsync(request);
        }
    }

    private void SendStatusMailAsync(FurniturePickupRequest request)
    {
        // .invalid = reservierte Test-Domain (Beispieldaten) – dorthin keine Mails
        if (string.IsNullOrEmpty(request.Email)
            || request.Email.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)) return;

        var name        = $"{request.FirstName} {request.LastName}".Trim();
        var orderNum    = $"#{request.OrderNumber}";
        var pickupDate  = request.PickupDate.ToString("dd. MMMM yyyy",
                            new System.Globalization.CultureInfo("de-CH"));
        var noteSection = string.IsNullOrEmpty(request.AdminNote) ? "" :
            $"<tr style='background:#f8fafc;'><td style='padding:6px 12px;color:#64748b;'>Notiz vom Admin:</td><td style='padding:6px 12px;font-style:italic;'>{request.AdminNote}</td></tr>";

        var (icon, statusText, color, bgColor) = request.Status switch
        {
            PickupRequestStatus.Accepted => ("✅", "Akzeptiert",  "#15803d", "#dcfce7"),
            PickupRequestStatus.Rejected => ("❌", "Abgelehnt",   "#b91c1c", "#fee2e2"),
            PickupRequestStatus.Deleted  => ("🗑️", "Storniert",  "#64748b", "#f1f5f9"),
            _                            => ("⏳", "Ausstehend",  "#92400e", "#fef9c3")
        };

        var subject = $"{icon} Möbelabholung {orderNum} – {statusText}";
        var body = $"""
            <div style="font-family:sans-serif;max-width:540px;margin:0 auto;">
              <div style="background:linear-gradient(135deg,#1d4ed8,#2563eb);border-radius:16px 16px 0 0;padding:28px 32px;">
                <h1 style="margin:0;color:#fff;font-size:1.5rem;">Möbel Abholservice {icon}</h1>
              </div>
              <div style="background:#f8fafc;border:1px solid #e2e8f0;border-top:none;border-radius:0 0 16px 16px;padding:28px 32px;">
                <p style="color:#374151;margin-top:0;">Hallo <strong>{name}</strong>,<br>der Status deiner Möbelabholung hat sich geändert.</p>
                <div style="display:inline-block;padding:.4rem 1rem;border-radius:999px;background:{bgColor};color:{color};font-weight:800;font-size:1rem;margin-bottom:1.25rem;">
                  {icon} {statusText}
                </div>
                <table cellpadding="0" cellspacing="0" style="width:100%;border-collapse:collapse;margin:0 0 16px;background:#fff;border-radius:12px;overflow:hidden;border:1px solid #e2e8f0;">
                  <tr style="background:#dbeafe;">
                    <td colspan="2" style="padding:10px 12px;font-weight:800;color:#1d4ed8;font-size:.85rem;letter-spacing:.04em;text-transform:uppercase;">Deine Anfrage</td>
                  </tr>
                  <tr><td style="padding:6px 12px;color:#64748b;width:45%">Auftragsnummer:</td><td style="padding:6px 12px;font-weight:700;">{orderNum}</td></tr>
                  <tr style="background:#f8fafc;"><td style="padding:6px 12px;color:#64748b;">Abholdatum:</td><td style="padding:6px 12px;font-weight:700;">{pickupDate}</td></tr>
                  <tr><td style="padding:6px 12px;color:#64748b;">Adresse:</td><td style="padding:6px 12px;font-weight:700;">{request.Street}, {request.PostalCode} {request.City}</td></tr>
                  {noteSection}
                </table>
                <p style="color:#64748b;font-size:.9rem;">Bei Fragen kannst du dich direkt beim Flomi-Team melden.</p>
                <p style="color:#374151;font-weight:700;margin-bottom:0;">Freundliche Grüsse, das Flomi-Team 🙌</p>
              </div>
            </div>
            """;

        _mailQueue.Enqueue(m => m.SendAsync(request.Email, name, subject, body));
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
                IsEnabled        = settings.IsEnabled,
                PickupDateFrom   = settings.PickupDateFrom,
                PickupDateTo     = settings.PickupDateTo,
                EventId          = settings.EventId,
                DepartureAddress = settings.DepartureAddress
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
                    IsEnabled        = settings.IsEnabled,
                    PickupDateFrom   = settings.PickupDateFrom,
                    PickupDateTo     = settings.PickupDateTo,
                    EventId          = settings.EventId,
                    DepartureAddress = settings.DepartureAddress
                };
                await _context.FurniturePickupSettings.AddAsync(newEntry);
            }
            else
            {
                existing.IsEnabled        = settings.IsEnabled;
                existing.PickupDateFrom   = settings.PickupDateFrom;
                existing.PickupDateTo     = settings.PickupDateTo;
                existing.EventId          = settings.EventId;
                existing.DepartureAddress = settings.DepartureAddress;

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

    public async Task AssignVehicleAsync(int requestId, int? vehicleId)
    {
        var request = await _context.FurniturePickupRequests.FindAsync(requestId);
        if (request == null) return;
        if (request.AssignedVehicleId != vehicleId)
            request.RouteStopOrder = null;   // von Hand umgeteilt → Reihenfolge neu planen
        request.AssignedVehicleId = vehicleId;
        await _context.SaveChangesAsync();
    }

    public async Task AddImageAsync(int requestId, ProcessedImage image, string fileName, string? caption)
    {
        var entity = NewImage(image, fileName, caption);
        entity.FurniturePickupRequestId = requestId;
        _context.FurniturePickupImages.Add(entity);
        await _context.SaveChangesAsync();
    }

    private static FurniturePickupImage NewImage(ProcessedImage image, string fileName, string? caption) => new()
    {
        ContentType = ProcessedImage.ContentType,
        FileName    = Path.ChangeExtension(fileName, ".jpg"),
        Caption     = caption,
        Data        = new FurniturePickupImageData { ImageData = image.Data, ThumbnailData = image.Thumbnail }
    };

    // ════════════════════════════════════════════════════════════════════════
    // BILDER – Auslieferung, Löschfrist, Nachbearbeitung
    // ════════════════════════════════════════════════════════════════════════

    public async Task<(byte[] Data, string ContentType)?> GetImageContentAsync(int imageId, bool thumbnail)
    {
        var images = _context.FurniturePickupImages.AsNoTracking().Where(i => i.Id == imageId);

        if (thumbnail)
        {
            var thumb = await images.Select(i => i.Data.ThumbnailData).FirstOrDefaultAsync();
            if (thumb is { Length: > 0 }) return (thumb, ProcessedImage.ContentType);
            // Altbild ohne Vorschau → volles Bild ausliefern
        }

        var full = await images.Select(i => new { i.Data.ImageData, i.ContentType }).FirstOrDefaultAsync();
        return full == null ? null : (full.ImageData, full.ContentType);
    }

    public async Task<int> DeleteExpiredImagesAsync(DateTime now)
    {
        var cutoff = now.AddMonths(-PhotoRetentionMonths);
        var deleted = 0;

        while (true)
        {
            // Bilddaten mitladen: bei Table Splitting müssen beide Teile gelöscht werden
            var batch = await _context.FurniturePickupImages
                .Include(i => i.Data)
                .Where(i => (i.FurniturePickupRequest.EventId != null
                                ? i.FurniturePickupRequest.Event!.Date
                                : i.FurniturePickupRequest.PickupDate) < cutoff)
                .OrderBy(i => i.Id)
                .Take(20)
                .ToListAsync();

            if (batch.Count == 0) break;

            _context.FurniturePickupImages.RemoveRange(batch);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            deleted += batch.Count;
        }

        if (deleted > 0)
            _logger.LogInformation("Löschfrist: {Count} Möbelfotos gelöscht (Event älter als {Months} Monate).",
                deleted, PhotoRetentionMonths);
        return deleted;
    }

    public async Task<int> ReprocessLegacyImagesAsync(int maxCount)
    {
        var ids = await _context.FurniturePickupImages
            .Where(i => i.Data.ThumbnailData == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .Take(maxCount)
            .ToListAsync();

        foreach (var id in ids)
        {
            var image = await _context.FurniturePickupImages
                .Include(i => i.Data)
                .FirstAsync(i => i.Id == id);

            var processed = PickupImageProcessor.Process(image.Data.ImageData);
            if (processed == null)
            {
                // Unlesbares Altbild unverändert lassen, aber nicht jedes Mal neu versuchen
                _logger.LogWarning("Möbelfoto {Id} konnte nicht aufbereitet werden und bleibt unverändert.", id);
                image.Data.ThumbnailData = Array.Empty<byte>();
            }
            else
            {
                image.Data.ImageData     = processed.Data;
                image.Data.ThumbnailData = processed.Thumbnail;
                image.ContentType        = ProcessedImage.ContentType;
                image.FileName           = Path.ChangeExtension(image.FileName, ".jpg");
            }

            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        return ids.Count;
    }
}