// Services/ExportService.cs
using ClosedXML.Excel;
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

    private async Task<byte[]> _OldExportAppointmentsAsync(int? eventId = null)
    {
        var query = _db.Appointments
            .Include(a => a.User)
            .Include(a => a.FamilyMember)
            .Include(a => a.Area)
                .ThenInclude(ar => ar.Event)
            .Include(a => a.Area)
                .ThenInclude(ar => ar.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .AsQueryable();

        if (eventId.HasValue)
            query = query.Where(a => a.Area.EventId == eventId.Value);

        var data = await query
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Area.AreaTemplate!.Name)
            .ThenBy(a => a.Area.TimeSlot)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Anmeldungen");

        // ── Spalten-Header (Zeile 1) ──────────────────────────────────────────
        var headers = new[] { "Ressorts", "Name", "Pfadi Name", "Kommentar", "Alter" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        StyleHeader(ws, headers.Length);

        // ── Gruppieren nach Kategorie / Bereich + Zeitslot ───────────────────
        var grouped = data
            .GroupBy(a => new
            {
                Category = a.Area.AreaTemplate?.AreaCategory?.Name ?? "",
                Name     = a.Area.AreaTemplate?.Name ?? "",
                a.Area.TimeSlot
            })
            .OrderBy(g => g.Key.Category)
            .ThenBy(g => g.Key.Name)
            .ThenBy(g => g.Key.TimeSlot);

        int row = 2;

        foreach (var group in grouped)
        {
            string ressortLabel = string.IsNullOrEmpty(group.Key.Category)
                ? $"{group.Key.Name} ({group.Key.TimeSlot})"
                : $"[{group.Key.Category}] {group.Key.Name} ({group.Key.TimeSlot})";
            ws.Cell(row, 1).Value = ressortLabel;

            // Ganze Zeile fett + hellgrauer Hintergrund
            var headerRange = ws.Range(row, 1, row, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#BDBDBD");

            row++;

            // ── Personen in diesem Ressort ────────────────────────────────────
            var appointments = group.ToList();

            if (!appointments.Any())
            {
                // Leere Zeile wenn niemand angemeldet
                row++;
            }
            else
            {
                foreach (var a in appointments)
                {
                    string vorname    = a.FamilyMember?.FirstName ?? a.User.FirstName;
                    string nachname   = a.FamilyMember?.LastName  ?? a.User.LastName;
                    string pfadiName  = a.FamilyMember?.Pfadiname ?? a.User.Pfadiname ?? "";
                    string kommentar  = a.Comment ?? "";

                    // Alter berechnen
                    DateTime? birthday = a.FamilyMember?.Birthday ?? a.User.Birthday;
                    string alter = "";
                    if (birthday.HasValue)
                    {
                        var today = DateTime.Today;
                        int age = today.Year - birthday.Value.Year;
                        if (birthday.Value.Date > today.AddYears(-age)) age--;
                        alter = age.ToString();
                    }

                    ws.Cell(row, 1).Value = "";  // Ressort-Spalte leer lassen
                    ws.Cell(row, 2).Value = $"{vorname} {nachname}";
                    ws.Cell(row, 3).Value = pfadiName;
                    ws.Cell(row, 4).Value = kommentar;
                    ws.Cell(row, 5).Value = alter;

                    // Alter rechtsbündig
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Zebra-Streifen für Lesbarkeit
                    if (row % 2 == 0)
                        ws.Range(row, 1, row, headers.Length)
                        .Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                    row++;
                }

                // Leerzeile nach jeder Gruppe
                row++;
            }
        }

        // ── Styling & Breiten ─────────────────────────────────────────────────
        ws.Column(1).Width = 35;  // Ressorts
        ws.Column(2).Width = 25;  // Name
        ws.Column(3).Width = 18;  // Pfadi Name
        ws.Column(4).Width = 30;  // Kommentar
        ws.Column(5).Width = 8;   // Alter

        // Rahmen um alles
        if (row > 2)
        {
            ws.Range(1, 1, row - 1, headers.Length)
            .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, row - 1, headers.Length)
            .Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
        }

        ws.SheetView.FreezeRows(1);

        return WorkbookToBytes(wb);
    }

    public Task<byte[]> ExportGastroAppointmentsAsync(int? eventId = null)
        => ExportAppointmentsByCategoryAsync(eventId, "Gastro", "Anmeldungen Gastro");

    private async Task<byte[]> _OldExportGastroAppointmentsAsync(int? eventId = null)
    {
        var query = _db.Appointments
            .Include(a => a.User)
            .Include(a => a.FamilyMember)
            .Include(a => a.Area)
                .ThenInclude(ar => ar.Event)
            .Include(a => a.Area)
                .ThenInclude(ar => ar.AreaTemplate)
                    .ThenInclude(t => t!.AreaCategory)
            .AsQueryable();

        if (eventId.HasValue)
            query = query.Where(a => a.Area.EventId == eventId.Value);

        var data = await query
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Area.AreaTemplate!.Name)
            .ThenBy(a => a.Area.TimeSlot)
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Anmeldungen Gastro");

        // ── Spalten-Header (Zeile 1) ──────────────────────────────────────────
        var headers = new[] { "Ressorts", "Name", "Pfadi Name", "Kommentar", "Alter" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        StyleHeader(ws, headers.Length);

        // ── Gruppieren nach Kategorie / Bereich + Zeitslot ───────────────────
        var grouped = data
            .GroupBy(a => new
            {
                Category = a.Area.AreaTemplate?.AreaCategory?.Name ?? "",
                Name     = a.Area.AreaTemplate?.Name ?? "",
                a.Area.TimeSlot
            })
            .OrderBy(g => g.Key.Category)
            .ThenBy(g => g.Key.Name)
            .ThenBy(g => g.Key.TimeSlot);

        int row = 2;

        foreach (var group in grouped)
        {
            string ressortLabel = string.IsNullOrEmpty(group.Key.Category)
                ? $"{group.Key.Name} ({group.Key.TimeSlot})"
                : $"[{group.Key.Category}] {group.Key.Name} ({group.Key.TimeSlot})";
            ws.Cell(row, 1).Value = ressortLabel;

            // Ganze Zeile fett + hellgrauer Hintergrund
            var headerRange = ws.Range(row, 1, row, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#BDBDBD");

            row++;

            // ── Personen in diesem Ressort ────────────────────────────────────
            var appointments = group.ToList();

            if (!appointments.Any())
            {
                // Leere Zeile wenn niemand angemeldet
                row++;
            }
            else
            {
                foreach (var a in appointments)
                {
                    string vorname    = a.FamilyMember?.FirstName ?? a.User.FirstName;
                    string nachname   = a.FamilyMember?.LastName  ?? a.User.LastName;
                    string pfadiName  = a.FamilyMember?.Pfadiname ?? a.User.Pfadiname ?? "";
                    string kommentar  = a.Comment ?? "";

                    // Alter berechnen
                    DateTime? birthday = a.FamilyMember?.Birthday ?? a.User.Birthday;
                    string alter = "";
                    if (birthday.HasValue)
                    {
                        var today = DateTime.Today;
                        int age = today.Year - birthday.Value.Year;
                        if (birthday.Value.Date > today.AddYears(-age)) age--;
                        alter = age.ToString();
                    }

                    ws.Cell(row, 1).Value = "";  // Ressort-Spalte leer lassen
                    ws.Cell(row, 2).Value = $"{vorname} {nachname}";
                    ws.Cell(row, 3).Value = pfadiName;
                    ws.Cell(row, 4).Value = kommentar;
                    ws.Cell(row, 5).Value = alter;

                    // Alter rechtsbündig
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Zebra-Streifen für Lesbarkeit
                    if (row % 2 == 0)
                        ws.Range(row, 1, row, headers.Length)
                        .Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                    row++;
                }

                // Leerzeile nach jeder Gruppe
                row++;
            }
        }

        // ── Styling & Breiten ─────────────────────────────────────────────────
        ws.Column(1).Width = 35;  // Ressorts
        ws.Column(2).Width = 25;  // Name
        ws.Column(3).Width = 18;  // Pfadi Name
        ws.Column(4).Width = 30;  // Kommentar
        ws.Column(5).Width = 8;   // Alter

        // Rahmen um alles
        if (row > 2)
        {
            ws.Range(1, 1, row - 1, headers.Length)
            .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, row - 1, headers.Length)
            .Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
        }

        ws.SheetView.FreezeRows(1);

        return WorkbookToBytes(wb);
    }


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

        // ── Users ─────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportUsersAsync()
        {
            var data = await _db.Users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Benutzer");

            var headers = new[]
            {
                "ID", "Vorname", "Nachname", "Pfadiname", "Stufe",
                "Email", "Telefon", "Geburtstag", "E-Mail Benachrichtigungen"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            StyleHeader(ws, headers.Length);

            int row = 2;
            foreach (var u in data)
            {
                ws.Cell(row, 1).Value = u.Id;
                ws.Cell(row, 2).Value = u.FirstName;
                ws.Cell(row, 3).Value = u.LastName;
                ws.Cell(row, 4).Value = u.Pfadiname ?? "–";
                ws.Cell(row, 5).Value = u.Stufe     ?? "–";
                ws.Cell(row, 6).Value = u.Email;
                ws.Cell(row, 7).Value = u.PhoneNumber ?? "–";
                ws.Cell(row, 8).Value = u.Birthday.HasValue
                    ? u.Birthday.Value.ToString("dd.MM.yyyy")
                    : "–";
                ws.Cell(row, 9).Value = u.EmailNotificationsEnabled ? "Ja" : "Nein";
                row++;
            }

            StyleTable(ws, headers.Length, row - 1);
            return WorkbookToBytes(wb);
        }

        // ── Family Members ────────────────────────────────────────────────────
        public async Task<byte[]> ExportFamilyMembersAsync()
        {
            var data = await _db.FamilyMembers
                .Include(f => f.User)
                .OrderBy(f => f.User.LastName)
                .ThenBy(f => f.LastName)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Familienmitglieder");

            var headers = new[]
            {
                "ID", "Konto Vorname", "Konto Nachname", "Konto Email",
                "Vorname", "Nachname", "Pfadiname", "Stufe", "Geburtstag"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            StyleHeader(ws, headers.Length);

            int row = 2;
            foreach (var f in data)
            {
                ws.Cell(row, 1).Value = f.Id;
                ws.Cell(row, 2).Value = f.User.FirstName;
                ws.Cell(row, 3).Value = f.User.LastName;
                ws.Cell(row, 4).Value = f.User.Email;
                ws.Cell(row, 5).Value = f.FirstName;
                ws.Cell(row, 6).Value = f.LastName;
                ws.Cell(row, 7).Value = f.Pfadiname ?? "–";
                ws.Cell(row, 8).Value = f.Stufe     ?? "–";
                ws.Cell(row, 9).Value = f.Birthday.ToString("dd.MM.yyyy");
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