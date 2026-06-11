// Services/ExportService.cs
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services
{
    public class ExportService : IExportService
    {
        private readonly ApplicationDbContext _db;

        public ExportService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── Shared: Anmeldungen nach Kategorie ───────────────────────────────
        private async Task<byte[]> ExportAppointmentsByCategoryAsync(
            int? eventId, string categoryName, string sheetName)
        {
            var query = _db.Appointments
                .Include(a => a.User)
                .Include(a => a.FamilyMember)
                .Include(a => a.Area)
                    .ThenInclude(ar => ar.AreaTemplate)
                        .ThenInclude(t => t!.AreaCategory)
                .Where(a => a.Status != AppointmentStatus.Cancelled
                         && a.Area.AreaTemplate != null
                         && a.Area.AreaTemplate.AreaCategory != null
                         && a.Area.AreaTemplate.AreaCategory.Name.ToLower() == categoryName.ToLower())
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(a => a.Area.EventId == eventId.Value);

            var data = await query
                .OrderBy(a => a.Area.AreaTemplate!.Name)
                .ThenBy(a => a.UseAlternativeSlot)
                .ToListAsync();

            using var wb  = new XLWorkbook();
            var ws = wb.Worksheets.Add(sheetName);

            var headers = new[] { "Ressort", "Zeitslot", "Name", "Pfadiname", "Kommentar", "Alter" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            StyleHeader(ws, headers.Length);

            int row = 2;
            var grouped = data
                .GroupBy(a => a.Area.AreaTemplate?.Name ?? "")
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                // Ressort-Header
                ws.Cell(row, 1).Value = group.Key;
                ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
                ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                row++;

                foreach (var a in group)
                {
                    var timeSlot = a.UseAlternativeSlot && !string.IsNullOrEmpty(a.Area.AlternativeTimeSlot)
                        ? a.Area.AlternativeTimeSlot
                        : a.Area.TimeSlot;

                    var vorname   = a.FamilyMember?.FirstName ?? a.User.FirstName;
                    var nachname  = a.FamilyMember?.LastName  ?? a.User.LastName;
                    var pfadi     = a.FamilyMember?.Pfadiname ?? a.User.Pfadiname ?? "–";
                    var kommentar = a.Comment ?? "";

                    DateTime? birthday = a.FamilyMember?.Birthday ?? a.User.Birthday;
                    string alter = "";
                    if (birthday.HasValue)
                    {
                        int age = DateTime.Today.Year - birthday.Value.Year;
                        if (birthday.Value.Date > DateTime.Today.AddYears(-age)) age--;
                        alter = age.ToString();
                    }

                    ws.Cell(row, 1).Value = "";
                    ws.Cell(row, 2).Value = timeSlot;
                    ws.Cell(row, 3).Value = $"{vorname} {nachname}";
                    ws.Cell(row, 4).Value = pfadi;
                    ws.Cell(row, 5).Value = kommentar;
                    ws.Cell(row, 6).Value = alter;
                    ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    if (row % 2 == 0)
                        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                    row++;
                }
                row++; // Leerzeile nach Gruppe
            }

            ws.Column(1).Width = 28;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 22;
            ws.Column(4).Width = 16;
            ws.Column(5).Width = 30;
            ws.Column(6).Width = 8;

            if (row > 2)
            {
                ws.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            }
            ws.SheetView.FreezeRows(1);
            return WorkbookToBytes(wb);
        }

        // ── Appointments ──────────────────────────────────────────────────────
    public Task<byte[]> ExportAppointmentsAsync(int? eventId = null)
        => ExportAppointmentsByCategoryAsync(eventId, "Verkauf", "Anmeldungen Verkauf");

    public Task<byte[]> ExportGastroAppointmentsAsync(int? eventId = null)
        => ExportAppointmentsByCategoryAsync(eventId, "Gastro", "Anmeldungen Gastro");


        // ── Pickup Requests ───────────────────────────────────────────────────
        public async Task<byte[]> ExportPickupRequestsAsync(int? eventId = null)
        {
            var query = _db.FurniturePickupRequests
                .Include(r => r.User)
                .Include(r => r.Event)
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(r => r.EventId == eventId.Value);

            var data = await query
                .OrderBy(r => r.Event != null ? r.Event.Name : "zzz")
                .ThenBy(r => r.PickupDate)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Abholungen");

            var headers = new[]
            {
                "ID", "Auftragsnr.", "Event", "Vorname", "Nachname",
                "Strasse", "PLZ", "Ort", "Abholdatum",
                "Beschreibung", "Status", "Admin-Notiz", "Eingereicht am"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            StyleHeader(ws, headers.Length);

            int row = 2;
            foreach (var r in data)
            {
                ws.Cell(row, 1).Value  = r.Id;
                ws.Cell(row, 2).Value  = r.OrderNumber;
                ws.Cell(row, 3).Value  = r.Event?.Name ?? "–";
                ws.Cell(row, 4).Value  = r.User.FirstName;
                ws.Cell(row, 5).Value  = r.User.LastName;
                ws.Cell(row, 6).Value  = r.Street;
                ws.Cell(row, 7).Value  = r.PostalCode;
                ws.Cell(row, 8).Value  = r.City;
                ws.Cell(row, 9).Value  = r.PickupDate.ToString("dd.MM.yyyy");
                ws.Cell(row, 10).Value = r.Description;
                ws.Cell(row, 11).Value = r.Status.ToString();
                ws.Cell(row, 12).Value = r.AdminNote ?? "–";
                ws.Cell(row, 13).Value = r.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                row++;
            }

            StyleTable(ws, headers.Length, row - 1);
            return WorkbookToBytes(wb);
        }

        // ── Fahrzeuge ─────────────────────────────────────────────────────────
        public async Task<byte[]> ExportVehicleAssignmentsAsync(int? eventId = null)
        {
            var dates = await _db.AssignmentDates
                .Include(d => d.Assignment).ThenInclude(a => a.Vehicle)
                .Include(d => d.Assignment).ThenInclude(a => a.Event)
                .Where(d => eventId == null || d.Assignment.EventId == eventId)
                .OrderBy(d => d.Date)
                .ThenBy(d => d.Assignment.Vehicle.OwnerName)
                .AsNoTracking()
                .ToListAsync();

            // User-Lookup für Fahrer/Beifahrer
            var userIds = dates
                .SelectMany(d => new[] { d.DriverUserId, d.HelperUserId }.Where(id => id != null))
                .Distinct().ToList();
            var users = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Pfadiname, u.PhoneNumber })
                .ToListAsync();
            string UserName(string? uid) => uid == null ? "" :
                users.FirstOrDefault(u => u.Id == uid) is { } u
                    ? $"{u.FirstName} {u.LastName}"
                    : "";
            string UserPhone(string? uid) => uid == null ? "" :
                users.FirstOrDefault(u => u.Id == uid)?.PhoneNumber ?? "";

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fahrzeuge");

            // Titel
            var eventName = eventId.HasValue
                ? (await _db.Events.FindAsync(eventId.Value))?.Name ?? "Fahrzeuge"
                : "Fahrzeuge";
            ws.Cell(1, 1).Value = eventName;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;

            var headers = new[]
            {
                "Fahrzeughalter", "Tel.-Nummer", "Fahrer", "Fahrer Tel.",
                "Beifahrer", "Bereit ab", "Abgeholt durch", "Zurückgebracht durch"
            };

            int row = 3;
            DateTime? currentDate = null;

            foreach (var d in dates)
            {
                // Datums-Abschnittsheader
                if (d.Date.Date != currentDate)
                {
                    currentDate = d.Date.Date;
                    var dateLabel = d.Date.ToString("ddd dd.MM.yyyy", new System.Globalization.CultureInfo("de-CH"));
                    ws.Cell(row, 1).Value = dateLabel;
                    var dateRange = ws.Range(row, 1, row, headers.Length);
                    dateRange.Style.Font.Bold = true;
                    dateRange.Style.Font.FontSize = 12;
                    dateRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
                    dateRange.Style.Font.FontColor = XLColor.White;
                    dateRange.Merge();
                    row++;

                    // Spalten-Header
                    for (int i = 0; i < headers.Length; i++)
                        ws.Cell(row, i + 1).Value = headers[i];
                    var hRange = ws.Range(row, 1, row, headers.Length);
                    hRange.Style.Font.Bold = true;
                    hRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                    hRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                var driverName  = !string.IsNullOrEmpty(d.DriverName) ? d.DriverName  : UserName(d.DriverUserId);
                var driverPhone = !string.IsNullOrEmpty(d.DriverPhone) ? d.DriverPhone : UserPhone(d.DriverUserId);
                var helperName  = !string.IsNullOrEmpty(d.HelperName) ? d.HelperName  : UserName(d.HelperUserId);

                ws.Cell(row, 1).Value = d.Assignment.Vehicle.OwnerName;
                ws.Cell(row, 2).Value = d.Assignment.Vehicle.OwnerPhone ?? "";
                ws.Cell(row, 3).Value = driverName;
                ws.Cell(row, 4).Value = driverPhone;
                ws.Cell(row, 5).Value = helperName;
                ws.Cell(row, 6).Value = d.ReadyFrom ?? "";
                ws.Cell(row, 7).Value = d.PickedUpBy ?? "";
                ws.Cell(row, 8).Value = d.ReturnedBy ?? "";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                row++;
            }

            ws.Column(1).Width = 22; ws.Column(2).Width = 16;
            ws.Column(3).Width = 20; ws.Column(4).Width = 16;
            ws.Column(5).Width = 20; ws.Column(6).Width = 12;
            ws.Column(7).Width = 24; ws.Column(8).Width = 24;
            ws.SheetView.FreezeRows(2);
            return WorkbookToBytes(wb);
        }

        // ── Route Export (pro Fahrzeug, pro Tag) ─────────────────────────────
        public async Task<byte[]> ExportRouteAsync(int? eventId, DateTime date)
        {
            var query = _db.FurniturePickupRequests
                .Include(r => r.AssignedVehicle)
                .Where(r => r.PickupDate.Date == date.Date
                         && (r.Status == PickupRequestStatus.Pending
                          || r.Status == PickupRequestStatus.Accepted))
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(r => r.EventId == eventId.Value);

            var data = await query
                .OrderBy(r => r.AssignedVehicle != null ? r.AssignedVehicle.OwnerName : "zzz")
                .ThenBy(r => r.PostalCode)
                .ThenBy(r => r.Street)
                .AsNoTracking()
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Route");

            var culture  = new System.Globalization.CultureInfo("de-CH");
            var dateLabel = date.ToString("dddd, dd.MM.yyyy", culture);

            // Title row
            ws.Cell(1, 1).Value = $"Routenplan – {dateLabel}";
            var titleRange = ws.Range(1, 1, 1, 10);
            titleRange.Merge();
            titleRange.Style.Font.Bold      = true;
            titleRange.Style.Font.FontSize  = 14;
            titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            var headers = new[] { "#", "Auftragsnr.", "Name", "Telefon", "Strasse", "PLZ", "Ort", "Möbelbeschreibung", "Status", "Admin-Notiz" };

            int row = 2;

            void WriteVehicleSection(string vehicleName, string? vehiclePhone, List<FurniturePickupRequest> pickups)
            {
                // Vehicle header
                ws.Cell(row, 1).Value = $"🚗  {vehicleName}" + (string.IsNullOrEmpty(vehiclePhone) ? "" : $"   📞 {vehiclePhone}");
                var vRange = ws.Range(row, 1, row, headers.Length);
                vRange.Merge();
                vRange.Style.Font.Bold      = true;
                vRange.Style.Font.FontSize  = 11;
                vRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
                vRange.Style.Font.FontColor = XLColor.White;
                row++;

                // Column headers
                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(row, i + 1).Value = headers[i];
                var hRange = ws.Range(row, 1, row, headers.Length);
                hRange.Style.Font.Bold = true;
                hRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                hRange.Style.Border.BottomBorder  = XLBorderStyleValues.Thin;
                row++;

                // Data rows
                int stopNr = 1;
                foreach (var r in pickups)
                {
                    ws.Cell(row, 1).Value  = stopNr++;
                    ws.Cell(row, 2).Value  = r.OrderNumber;
                    ws.Cell(row, 3).Value  = $"{r.FirstName} {r.LastName}".Trim();
                    ws.Cell(row, 4).Value  = r.PhoneNumber ?? "";
                    ws.Cell(row, 5).Value  = r.Street;
                    ws.Cell(row, 6).Value  = r.PostalCode;
                    ws.Cell(row, 7).Value  = r.City;
                    ws.Cell(row, 8).Value  = r.Description ?? "";
                    ws.Cell(row, 9).Value  = r.Status == PickupRequestStatus.Accepted ? "Akzeptiert" : "Ausstehend";
                    ws.Cell(row, 10).Value = r.AdminNote ?? "";

                    if (stopNr % 2 == 0)
                        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    row++;
                }

                row++; // blank row between vehicles
            }

            // Assigned pickups grouped by vehicle
            var assigned = data.Where(r => r.AssignedVehicle != null)
                               .GroupBy(r => r.AssignedVehicle!.Id);
            foreach (var group in assigned)
            {
                var v = group.First().AssignedVehicle!;
                WriteVehicleSection(v.OwnerName, v.OwnerPhone, group.ToList());
            }

            // Unassigned
            var unassigned = data.Where(r => r.AssignedVehicle == null).ToList();
            if (unassigned.Any())
                WriteVehicleSection("Nicht zugeteilt", null, unassigned);

            // Column widths
            ws.Column(1).Width  = 5;
            ws.Column(2).Width  = 12;
            ws.Column(3).Width  = 24;
            ws.Column(4).Width  = 18;
            ws.Column(5).Width  = 28;
            ws.Column(6).Width  = 7;
            ws.Column(7).Width  = 18;
            ws.Column(8).Width  = 32;
            ws.Column(9).Width  = 14;
            ws.Column(10).Width = 28;
            ws.SheetView.FreezeRows(1);

            return WorkbookToBytes(wb);
        }

        // ── Aufräum-Liste (Fotos + Auftragsnummer) ────────────────────────────
        public async Task<byte[]> ExportPickupCleanupAsync(int? eventId = null)
        {
            var query = _db.FurniturePickupRequests
                .Include(r => r.Images)
                .Include(r => r.Event)
                .Where(r => r.Status != PickupRequestStatus.Deleted)
                .AsQueryable();

            if (eventId.HasValue)
                query = query.Where(r => r.EventId == eventId.Value);

            var data = await query
                .OrderBy(r => r.OrderNumber)
                .AsNoTracking()
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Aufräumliste");

            // Druckeinstellungen A4
            ws.PageSetup.PaperSize        = XLPaperSize.A4Paper;
            ws.PageSetup.PageOrientation  = XLPageOrientation.Portrait;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.Margins.Left     = 0.5;
            ws.PageSetup.Margins.Right    = 0.5;
            ws.PageSetup.Margins.Top      = 0.6;
            ws.PageSetup.Margins.Bottom   = 0.6;

            // Spalten: 4 Foto-Spalten (je ~120px ≈ 17 units) + 1 Notizen-Spalte
            const int    photoCols   = 4;
            const double photoColW   = 17.0;
            const int    photoWidthPx  = 115;
            const int    photoHeightPx = 85;
            const double photoRowPt    = 66.0; // ≈ 88px
            const int    totalCols   = photoCols + 1;

            for (int c = 1; c <= photoCols; c++)
                ws.Column(c).Width = photoColW;
            ws.Column(totalCols).Width = 22;

            // Titelzeile
            var eventName = eventId.HasValue
                ? (await _db.Events.FindAsync(eventId.Value))?.Name ?? "Alle Events"
                : "Alle Events";

            ws.Cell(1, 1).Value = $"Aufräumliste – {eventName}";
            var titleRange = ws.Range(1, 1, 1, totalCols);
            titleRange.Merge();
            titleRange.Style.Font.Bold            = true;
            titleRange.Style.Font.FontSize        = 13;
            titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
            titleRange.Style.Font.FontColor       = XLColor.White;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleRange.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 20;

            int row = 2;

            foreach (var req in data)
            {
                // ── Kopfzeile: Auftragsnummer + Person + Datum + Adresse ──
                ws.Cell(row, 1).Value =
                    $"#{req.OrderNumber}   ·   {req.FirstName} {req.LastName}   ·   {req.PickupDate:dd.MM.yyyy}   ·   {req.Street}, {req.PostalCode} {req.City}";
                var headerRange = ws.Range(row, 1, row, totalCols);
                headerRange.Merge();
                headerRange.Style.Font.Bold            = true;
                headerRange.Style.Font.FontSize        = 11;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                headerRange.Style.Font.FontColor       = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                headerRange.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                ws.Row(row).Height = 20;
                row++;

                // ── Beschreibungs-Zeile ──
                ws.Cell(row, 1).Value = req.Description ?? "–";
                var descRange = ws.Range(row, 1, row, photoCols);
                descRange.Merge();
                descRange.Style.Fill.BackgroundColor  = XLColor.FromHtml("#EFF6FF");
                descRange.Style.Font.FontSize         = 10;
                descRange.Style.Alignment.Vertical    = XLAlignmentVerticalValues.Center;
                descRange.Style.Alignment.WrapText    = true;

                ws.Cell(row, totalCols).Value = "Notizen:";
                ws.Cell(row, totalCols).Style.Font.Bold           = true;
                ws.Cell(row, totalCols).Style.Font.FontSize       = 9;
                ws.Cell(row, totalCols).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
                ws.Row(row).Height = 18;
                row++;

                // ── Foto-Zeile ──
                int photoRow = row;
                ws.Row(photoRow).Height = photoRowPt;

                if (req.Images.Any())
                {
                    int photoCol = 1;
                    foreach (var img in req.Images.Take(photoCols))
                    {
                        try
                        {
                            using var imgStream = new MemoryStream(img.ImageData);
                            var format = img.ContentType.ToLower() switch
                            {
                                "image/png" => XLPictureFormat.Png,
                                "image/gif" => XLPictureFormat.Gif,
                                _           => XLPictureFormat.Jpeg
                            };
                            var pic = ws.AddPicture(imgStream, format);
                            pic.MoveTo(ws.Cell(photoRow, photoCol));
                            pic.Width  = photoWidthPx;
                            pic.Height = photoHeightPx;

                            if (!string.IsNullOrEmpty(img.Caption))
                            {
                                ws.Cell(photoRow, photoCol).Value = img.Caption;
                                ws.Cell(photoRow, photoCol).Style.Font.FontSize = 7;
                                ws.Cell(photoRow, photoCol).Style.Font.Italic   = true;
                                ws.Cell(photoRow, photoCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Bottom;
                            }
                        }
                        catch { /* Beschädigte Bilder überspringen */ }
                        photoCol++;
                    }
                }
                else
                {
                    ws.Cell(photoRow, 1).Value = "Keine Fotos vorhanden";
                    ws.Cell(photoRow, 1).Style.Font.Italic    = true;
                    ws.Cell(photoRow, 1).Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                    ws.Cell(photoRow, 1).Style.Font.FontSize  = 9;
                }

                row++;

                // ── Trennzeile ──
                ws.Row(row).Height = 5;
                row++;
            }

            ws.SheetView.FreezeRows(1);
            return WorkbookToBytes(wb);
        }

        // ── Styling Helpers ───────────────────────────────────────────────────
        private static void StyleHeader(IXLWorksheet ws, int colCount)
        {
            var headerRow = ws.Range(1, 1, 1, colCount);
            headerRow.Style.Fill.BackgroundColor     = XLColor.FromHtml("#1D4ED8");
            headerRow.Style.Font.FontColor           = XLColor.White;
            headerRow.Style.Font.Bold                = true;
            headerRow.Style.Font.FontSize            = 11;
            headerRow.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Center;
            headerRow.Style.Border.BottomBorder      = XLBorderStyleValues.Medium;
            headerRow.Style.Border.BottomBorderColor = XLColor.FromHtml("#1E40AF");
        }

        private static void StyleTable(IXLWorksheet ws, int colCount, int lastRow)
        {
            if (lastRow < 2) return;

            for (int r = 2; r <= lastRow; r++)
            {
                if (r % 2 == 0)
                    ws.Row(r).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            var tableRange = ws.Range(1, 1, lastRow, colCount);
            tableRange.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            tableRange.Style.Border.InsideBorder       = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorderColor  = XLColor.FromHtml("#E2E8F0");

            ws.Range(2, 1, lastRow, colCount).Style.Font.FontSize = 10;
            ws.Columns(1, colCount).AdjustToContents();

            for (int c = 1; c <= colCount; c++)
            {
                if (ws.Column(c).Width < 12)
                    ws.Column(c).Width = 12;
            }

            ws.SheetView.FreezeRows(1);
        }

        private static byte[] WorkbookToBytes(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}