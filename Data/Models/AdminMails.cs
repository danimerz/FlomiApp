namespace FlomiApp.Data.Models;

/// <summary>Inhalt der Admin-Mail «Neues Konto». Wird vor dem Einreihen in die Mail-Queue
/// fertig zusammengestellt, damit die Queue keine EF-Entitäten festhält.</summary>
public record AdminNewUserMail(
    string    FullName,
    string?   Pfadiname,
    string    Stufe,
    DateTime? Birthday,
    string    Email,
    bool      EmailConfirmed,
    string?   PhoneNumber,
    string    Method,       // «E-Mail und Passwort», «Google», …
    DateTime  CreatedAtUtc,
    int       TotalUsers,
    string?   AdminUrl);

/// <summary>Inhalt der Admin-Mail bei einer Anmeldung oder Stornierung für einen Bereich.</summary>
public record AdminRegistrationMail(
    bool      Cancelled,
    string    PersonName,
    string?   PersonStufe,
    DateTime? PersonBirthday,
    bool      ForFamilyMember,
    string    AccountName,
    string    AccountEmail,
    string?   AccountPhone,
    string    EventName,
    string    AreaName,
    string?   CategoryName,
    DateTime? Date,
    string    TimeSlot,
    string?   Comment,
    int       Registered,       // Belegung des Zeitfensters nach der Änderung
    int       Capacity,
    int       AccountRegistrationsInEvent,
    DateTime  AtUtc,
    string?   AdminUrl);
