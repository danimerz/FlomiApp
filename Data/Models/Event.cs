using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlomiApp.Data.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsActive        { get; set; } = true;
    public bool CheckInEnabled  { get; set; } = false;

    /// <summary>Beispieldaten zum Testen der Routenplanung – lassen sich gesammelt wieder entfernen.</summary>
    public bool IsSample        { get; set; } = false;

    // ── Öffentliche Angaben zum Markt (Startseite und Google-Veranstaltung) ──

    /// <summary>Name des Orts, z. B. «Dreifachturnhalle Loreto».</summary>
    [MaxLength(200)] public string? LocationName { get; set; }
    [MaxLength(200)] public string? Street       { get; set; }
    [MaxLength(10)]  public string? PostalCode   { get; set; }
    [MaxLength(100)] public string? City         { get; set; }

    /// <summary>Markt geöffnet ab / bis (Ortszeit). Leer = ganztägig am Event-Datum.</summary>
    public DateTime? MarketStart { get; set; }
    public DateTime? MarketEnd   { get; set; }

    [NotMapped]
    public bool HasLocation => !string.IsNullOrWhiteSpace(LocationName) || !string.IsNullOrWhiteSpace(City);

    /// <summary>«Turnhalle, Strasse 1, 6300 Zug» – leere Teile werden weggelassen.</summary>
    [NotMapped]
    public string LocationText => string.Join(", ", new[]
        {
            LocationName,
            Street,
            string.Join(" ", new[] { PostalCode, City }.Where(s => !string.IsNullOrWhiteSpace(s))),
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
