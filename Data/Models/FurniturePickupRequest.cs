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