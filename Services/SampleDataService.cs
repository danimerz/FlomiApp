// Services/SampleDataService.cs
using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FlomiApp.Services;

public interface ISampleDataService
{
    /// <summary>Gibt es Beispieldaten für die Routenplanung?</summary>
    Task<bool> HasRouteSampleAsync();

    /// <summary>
    /// Legt einen (für Freiwillige unsichtbaren) Beispiel-Event mit Fahrzeugen und akzeptierten
    /// Möbelabholungen an. Bestehende Beispieldaten werden vorher ersetzt.
    /// Liefert Event-Id und Abholdatum.
    /// </summary>
    Task<(int EventId, DateTime PickupDate)> CreateRouteSampleAsync();

    /// <summary>Entfernt alle Beispieldaten wieder (Event, Fahrzeuge, Abholungen, Einstellungen).</summary>
    Task RemoveRouteSampleAsync();
}

public class SampleDataService : ISampleDataService
{
    public const string SampleEventName      = "Beispiel-Flomi (Routenplanung)";
    public const string SampleStartAddress   = "Gewerbestrasse 1, 6314 Unterägeri";

    // Reale Adressen im Kanton Zug, damit die Adresssuche funktioniert – Personen sind erfunden.
    // Die letzte Adresse gibt es absichtlich nicht: so lässt sich die Warnung testen.
    private static readonly (string Name, string Street, string PostalCode, string City, string Description, string? Note)[] SamplePickups =
    {
        ("Anna Beispiel",    "Zugerstrasse 10",       "6314", "Unterägeri", "Sofa 3-Plätzer, Stoff grau",          null),
        ("Bruno Muster",     "Seestrasse 2",          "6314", "Unterägeri", "2 Nachttischli und eine Stehlampe",   null),
        ("Carla Test",       "Gewerbestrasse 3",      "6314", "Unterägeri", "Kommode Eiche, 4 Schubladen",         null),
        ("Daniel Probe",     "Wilbrunnenstrasse 25",  "6314", "Unterägeri", "Kinderbett inkl. Matratze",           "Klingeln bei Probe-Huber"),
        ("Eva Exempel",      "Lorzenstrasse 2",       "6314", "Unterägeri", "Büchergestell, zerlegt",              null),
        ("Fritz Vorlage",    "Zugerstrasse 102",      "6314", "Unterägeri", "Esstisch ausziehbar + 4 Stühle",      "Erst ab 10 Uhr"),
        ("Gabi Demo",        "Hauptstrasse 1",        "6315", "Oberägeri",  "Velo Damen und Kindervelo",           null),
        ("Hans Platzhalter", "Alosenstrasse 2",       "6315", "Oberägeri",  "Kleiderschrank 2-türig",              "3. Stock ohne Lift"),
        ("Iris Muster",      "Morgartenstrasse 20",   "6315", "Oberägeri",  "Gartenmöbel: Tisch + 6 Stühle",       null),
        ("Jonas Beispiel",   "Gulmstrasse 5",         "6315", "Oberägeri",  "Schreibtisch mit Bürostuhl",          null),
        ("Karin Test",       "Zugerstrasse 30",       "6340", "Baar",       "Sessel und Salontisch",               null),
        ("Leo Probe",        "Dorfstrasse 10a",       "6340", "Baar",       "Sideboard weiss",                     null),
        ("Mia Exempel",      "Bahnhofstrasse 10",     "6300", "Zug",        "Spiegel gross, Bilderrahmen",         "Vorsicht zerbrechlich"),
        ("Nico Vorlage",     "Baarerstrasse 50",      "6300", "Zug",        "Ecksofa (2 Teile)",                   null),
        ("Olivia Demo",      "Neugasse 1",            "6300", "Zug",        "3 Kisten Geschirr",                   null),
        ("Paul Platzhalter", "Zugerbergstrasse 5",    "6319", "Allenwinden","Holztruhe",                           null),
        ("Rita Muster",      "Sennweidstrasse 51",    "6312", "Steinhausen","Babywagen und Hochstuhl",             null),
        ("Sven Beispiel",    "Seestrasse 27",         "6314", "Unterägeri", "Fernsehmöbel",                        null),
        ("Tina Test",        "Phantasieweg 99",       "6314", "Unterägeri", "Stehlampe",                           "Adresse gibt es nicht – Testfall"),
    };

    private static readonly (string Owner, string Contact)[] SampleVehicles =
    {
        ("Beispiel: Pfadi-Bus",           "Bus, viel Platz"),
        ("Beispiel: Kombi Familie Muster", "Kombi"),
        ("Beispiel: Anhänger Kurt",        "PW mit Anhänger"),
    };

    private readonly ApplicationDbContext _context;

    public SampleDataService(ApplicationDbContext context) => _context = context;

    public Task<bool> HasRouteSampleAsync() => _context.Events.AnyAsync(e => e.IsSample);

    public async Task<(int EventId, DateTime PickupDate)> CreateRouteSampleAsync()
    {
        await RemoveRouteSampleAsync();

        // Ein Samstag in rund drei Wochen; abgeholt wird am Freitag davor
        var eventDate  = DateTime.Today.AddDays(21);
        eventDate      = eventDate.AddDays(((int)DayOfWeek.Saturday - (int)eventDate.DayOfWeek + 7) % 7);
        var pickupDate = eventDate.AddDays(-1);

        var sampleEvent = new Event
        {
            Name        = SampleEventName,
            Date        = eventDate,
            Description = "Beispieldaten zum Testen der Routenplanung – unter Fahrzeuge → Routenplanung wieder entfernen.",
            IsActive    = false,   // für Freiwillige unsichtbar
            IsSample    = true
        };
        _context.Events.Add(sampleEvent);
        await _context.SaveChangesAsync();

        _context.FurniturePickupSettings.Add(new FurniturePickupSettings
        {
            EventId          = sampleEvent.Id,
            IsEnabled        = false,   // keine Anmeldungen von Freiwilligen
            DepartureAddress = SampleStartAddress
        });

        foreach (var (owner, contact) in SampleVehicles)
        {
            var vehicle = new Vehicle { OwnerName = owner, OwnerContact = contact, IsSample = true };
            _context.Vehicles.Add(vehicle);
            _context.VehicleAssignments.Add(new VehicleAssignment
            {
                Vehicle       = vehicle,
                EventId       = sampleEvent.Id,
                AssignedDates = { new AssignmentDate { Date = pickupDate, DriverName = "Beispiel-Fahrer" } }
            });
        }

        var nextOrder = Math.Max((await _context.FurniturePickupRequests.MaxAsync(r => (int?)r.OrderNumber) ?? 999) + 1, 1000);
        var number = 1;
        foreach (var (name, street, postalCode, city, description, note) in SamplePickups)
        {
            var parts = name.Split(' ', 2);
            _context.FurniturePickupRequests.Add(new FurniturePickupRequest
            {
                OrderNumber = nextOrder++,
                FirstName   = parts[0],
                LastName    = parts[1],
                // .invalid ist eine reservierte Domain – an diese Adressen gehen keine Mails
                Email       = $"beispiel{number++}@example.invalid",
                Street      = street,
                PostalCode  = postalCode,
                City        = city,
                Description = description,
                AdminNote   = note,
                PickupDate  = pickupDate,
                Status      = PickupRequestStatus.Accepted,
                EventId     = sampleEvent.Id,
                AcceptsDisposalFee = true
            });
        }

        await _context.SaveChangesAsync();
        return (sampleEvent.Id, pickupDate);
    }

    public async Task RemoveRouteSampleAsync()
    {
        var eventIds = await _context.Events.Where(e => e.IsSample).Select(e => e.Id).ToListAsync();

        _context.FurniturePickupRequests.RemoveRange(
            await _context.FurniturePickupRequests.Where(r => r.EventId != null && eventIds.Contains(r.EventId.Value)).ToListAsync());
        _context.FurniturePickupSettings.RemoveRange(
            await _context.FurniturePickupSettings.Where(s => s.EventId != null && eventIds.Contains(s.EventId.Value)).ToListAsync());
        _context.VehicleAssignments.RemoveRange(
            await _context.VehicleAssignments.Where(a => eventIds.Contains(a.EventId) || a.Vehicle.IsSample).ToListAsync());
        _context.Vehicles.RemoveRange(await _context.Vehicles.Where(v => v.IsSample).ToListAsync());
        _context.Events.RemoveRange(await _context.Events.Where(e => e.IsSample).ToListAsync());

        await _context.SaveChangesAsync();
    }
}
