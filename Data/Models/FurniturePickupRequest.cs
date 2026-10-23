// Data/Models/FurniturePickupRequest.cs
using System.ComponentModel.DataAnnotations;

namespace FlomiApp.Data.Models;

public class FurniturePickupRequest
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Auftragsnummer ab 1000
    public int OrderNumber { get; set; }

    // Vorausgefüllte Profildaten (Snapshot zum Zeitpunkt der Anfrage)
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Pfadiname { get; set; }

    // Abholadresse
    [Required]
    public string Street { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string PostalCode { get; set; } = string.Empty;

    // Möbel-Beschreibung
    [Required]
    public string Description { get; set; } = string.Empty;

    // Gewünschtes Abholdatum
    [Required]
    public DateTime PickupDate { get; set; } = DateTime.Today.AddDays(7);

    // Bilder (als Byte-Arrays in DB gespeichert)
    public List<FurniturePickupImage> Images { get; set; } = new();

    // Status & Verwaltung
    public PickupRequestStatus Status { get; set; } = PickupRequestStatus.Pending;

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? EventId { get; set; }

    public Event? Event { get; set; }

    // Fahrzeugzuweisung für Routenplanung
    public int? AssignedVehicleId { get; set; }
    public Vehicle? AssignedVehicle { get; set; }

    /// <summary>Position in der Tour des zugewiesenen Fahrzeugs (1 = erster Stopp); null = noch nicht geplant.</summary>
    public int? RouteStopOrder { get; set; }

    // Koordinaten der Abholadresse (WGS84), von der Routenplanung ermittelt.
    // GeocodedAddress hält fest, für welche Adresse sie gelten – ändert sich die
    // Adresse, wird neu gesucht. Koordinaten null + GeocodedAddress gesetzt = nicht gefunden.
    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }
    [MaxLength(300)]
    public string? GeocodedAddress { get; set; }

    // Einverständnis Entsorgungsgebühren
    public bool AcceptsDisposalFee { get; set; } = false;

}

public enum PickupRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Deleted
}