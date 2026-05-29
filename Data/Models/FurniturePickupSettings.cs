using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlomiApp.Data.Models
{
    public class FurniturePickupSettings
    {
        public int Id { get; set; }

        /// <summary>Ist der Abholservice aktiv?</summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>Frühestes Abholdatum das der User wählen kann</summary>
        public DateTime? PickupDateFrom { get; set; }

        /// <summary>Spätestes Abholdatum das der User wählen kann</summary>
        public DateTime? PickupDateTo { get; set; }

        /// <summary>Maximale Abholungen pro Tag – null = unbegrenzt</summary>
        public int? MaxPickupsPerDay { get; set; }

        /// <summary>Verknüpfter Event – null = kein Event zugewiesen</summary>
        public int? EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }
    }
}