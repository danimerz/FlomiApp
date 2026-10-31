namespace FlomiApp.Data.Models;

/// <summary>Inhalt der Erinnerungsmail am Vortag – eine Mail pro Konto, fertig zusammengestellt.</summary>
public record ReminderMail(
    string                     FirstName,
    DateTime                   Date,
    string                     EventName,
    IReadOnlyList<ReminderItem> Items,
    string?                    DashboardUrl,
    bool                       IsTest = false);

/// <summary>Ein Einsatz in der Erinnerung.</summary>
public record ReminderItem(
    int      AppointmentId,
    string   AreaName,
    string?  CategoryName,
    string   TimeSlot,
    bool     IsSpontaneous,
    string?  Location,
    string?  ForPerson,       // Familienmitglied; null = die Person selbst
    string?  Comment,
    string?  VehicleInfo,     // «Fahrzeug von Muster · Beifahrer: Marc»
    bool     WithQrCode);
