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
    }
}