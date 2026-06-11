// Services/IExportService.cs
using FlomiApp.Data.Models;

namespace FlomiApp.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportAppointmentsAsync(int? eventId = null);
        Task<byte[]> ExportGastroAppointmentsAsync(int? eventId = null);
        Task<byte[]> ExportPickupRequestsAsync(int? eventId = null);
        Task<byte[]> ExportVehicleAssignmentsAsync(int? eventId = null);
        Task<byte[]> ExportRouteAsync(int? eventId, DateTime date);

        /// <summary>Aufräum-Liste: pro Auftrag Foto(s) + Auftragsnummer + Beschreibung (für Ausdruck)</summary>
        Task<byte[]> ExportPickupCleanupAsync(int? eventId = null);
    }
}