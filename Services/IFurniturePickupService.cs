// Services/IFurniturePickupService.cs
using FlomiApp.Data.Models;
using FlomiApp.Helpers;

namespace FlomiApp.Services;

public interface IFurniturePickupService
{
    Task<FurniturePickupRequest> CreateRequestAsync(
        FurniturePickupRequest request,
        List<(ProcessedImage Image, string FileName)> images);

    Task<List<FurniturePickupRequest>> GetUserRequestsAsync(string userId);
    Task<List<FurniturePickupRequest>> GetAllRequestsAsync();
    Task<FurniturePickupRequest?> GetRequestByIdAsync(int id);
    Task UpdateStatusAsync(int id, PickupRequestStatus status, string? adminNote);
    Task DeleteRequestAsync(int id);

    // ── Settings ────────────────────────────────────────────────────────────
    /// <summary>Globale Fallback-Settings (Id = 1) – für Rückwärtskompatibilität</summary>
    Task<FurniturePickupSettings> GetSettingsAsync();

    /// <summary>Alle Settings-Einträge (eine pro Event)</summary>
    Task<List<FurniturePickupSettings>> GetAllSettingsAsync();

    /// <summary>Upsert – neu anlegen oder updaten je nach Id</summary>
    Task SaveSettingsAsync(FurniturePickupSettings settings);

    /// <summary>Settings-Eintrag anhand der Id löschen</summary>
    Task DeleteSettingsAsync(int id);

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Alle aktiven Events für Dropdown / Auswahl</summary>
    Task<List<Event>> GetAvailableEventsAsync();

    /// <summary>
    /// Gibt das aktive Settings-Objekt für einen bestimmten Event zurück.
    /// Null wenn kein aktives Setting für diesen Event existiert.
    /// </summary>
    Task<FurniturePickupSettings?> GetSettingsByEventIdAsync(int eventId);

    /// <summary>Zählt bestehende Abholungen (Pending/Accepted) für ein Datum + Event</summary>
    Task<int> GetPickupCountForDateAsync(int eventId, DateTime date);

    /// <summary>Weist einer Abholung ein Fahrzeug zu (null = Zuweisung aufheben)</summary>
    Task AssignVehicleAsync(int requestId, int? vehicleId);

    /// <summary>Fügt einem bestehenden Auftrag ein weiteres Foto hinzu (Nacherfassung durch Admin)</summary>
    Task AddImageAsync(int requestId, ProcessedImage image, string fileName, string? caption);

    // ── Bilder ───────────────────────────────────────────────────────────────
    /// <summary>Bilddaten für die Auslieferung; Vorschau fällt bei Altbildern aufs volle Bild zurück. Null wenn nicht vorhanden.</summary>
    Task<(byte[] Data, string ContentType)?> GetImageContentAsync(int imageId, bool thumbnail);

    /// <summary>Löscht Fotos von Anfragen, deren Event länger als die Löschfrist zurückliegt. Liefert die Anzahl.</summary>
    Task<int> DeleteExpiredImagesAsync(DateTime now);

    /// <summary>Verkleinert bis zu <paramref name="maxCount"/> Altbilder ohne Vorschau. Liefert die Anzahl bearbeiteter Bilder.</summary>
    Task<int> ReprocessLegacyImagesAsync(int maxCount);
}