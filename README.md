# FlomiApp

**FlomiApp** ist eine webbasierte Verwaltungsapplikation für den Flomi-Markt – einen Flohmarkt der Pfadfinderabteilung. Die App wurde mit **ASP.NET Core 8 Blazor Server** entwickelt und deckt die gesamte Organisation des Events ab: von der Benutzeranmeldung über die Bereichszuweisung bis hin zur Fahrzeugverwaltung und Möbelabholung.

---

## Inhaltsverzeichnis

- [Technologie-Stack](#technologie-stack)
- [Funktionsübersicht](#funktionsübersicht)
  - [Benutzerverwaltung](#benutzerverwaltung)
  - [Anmeldung zu Bereichen](#anmeldung-zu-bereichen)
  - [Benutzerdashboard](#benutzerdashboard)
  - [Möbel-Abholservice](#möbel-abholservice)
  - [Fahrzeugverwaltung](#fahrzeugverwaltung)
  - [Admin-Konfiguration](#admin-konfiguration)
  - [Admin-Anmeldeübersicht](#admin-anmeldeübersicht)
  - [Datenexport](#datenexport)
- [Rollen & Berechtigungen](#rollen--berechtigungen)
- [Installation & Setup](#installation--setup)
- [Konfiguration](#konfiguration)

---

## Technologie-Stack

| Komponente | Technologie |
|---|---|
| Framework | ASP.NET Core 8 Blazor Server |
| Datenbank | SQL Server (LocalDB / Full) |
| ORM | Entity Framework Core 8 |
| Authentifizierung | ASP.NET Core Identity |
| E-Mail | SMTP (konfigurierbar) |
| Excel-Export | ClosedXML |
| UI | Custom CSS mit Design-Token-System (Light/Dark Mode) |

---

## Funktionsübersicht

### Benutzerverwaltung

- **Registrierung & Login** über ASP.NET Identity
- **Benutzerprofil** mit Vorname, Nachname, Pfadiname, Stufe, Geburtstag, Telefon, E-Mail
- **Familienmitglieder** können pro Konto hinterlegt werden (eigene Anmeldungen für Kinder / Begleitpersonen)
- **E-Mail-Benachrichtigungen** können pro Benutzer aktiviert/deaktiviert werden
- **Admin-Rolle**: Erweiterte Verwaltungsrechte für ausgewählte Benutzer

---

### Anmeldung zu Bereichen

Benutzer können sich für **Einsatzbereiche** des Flomi-Events anmelden.

**Bereichs-Stammdaten (Vorlagen)** werden einmalig erfasst und enthalten:
- Name (z.B. «Geschirr», «Spielzeug», «Fahrer»)
- Kategorie (z.B. Verkauf, Sammeln, Sortieren, Fahrer)
- Mindestalter
- Standort

**Bereichs-Zuweisungen** verknüpfen eine Vorlage mit einem Event und definieren:
- Datum, Zeitslot
- Maximale Kapazität (oder «Unbegrenzt»)

**Anmelde-Regeln:**
- Kapazitätsprüfung (volle Bereiche können nicht gebucht werden)
- Altersminimum wird geprüft
- Verkaufs-Kategorie: max. eine Anmeldung pro Person / Event
- Zeitslot-Konflikt-Prüfung (keine doppelten Buchungen zur gleichen Zeit)
- Optionaler **Kommentar** bei der Anmeldung (z.B. «Komme mit eigenem Auto»)

**Admin-Hilfsfunktionen:**
- Bereiche können zwischen Events **kopiert** werden (alle Zuweisungen mit angepasstem Datum)
- Filter und Sortierung in der Übersichtstabelle

---

### Benutzerdashboard

Das Dashboard zeigt dem eingeloggten Benutzer:

- **Alle aktiven Anmeldungen** gruppiert nach Event und Datum (aufklappbar)
- **Kalenderansicht** (umschaltbar per Toggle): Monatskalender mit Terminen und Möbelabholungen als farbige Chips
- **Fahrzeugzuweisung**: Wurde der Benutzer durch den Admin einem Fahrzeug als Fahrer oder Beifahrer zugeteilt, erscheint das Fahrzeug (Halter-Name) direkt auf der Anmeldungskarte
- **Möbel-Abholungsanfragen** des Benutzers mit Status und Admin-Notiz
- **Absagen** von Anmeldungen mit automatischer Stornierungsmail
- **Kalender-Download** (.ics) für jeden Termin

---

### Möbel-Abholservice

Benutzer können Möbel zur Abholung anmelden.

**Benutzerseite:**
- Auswahl des Events
- Eingabe: Name, Telefon, Adresse, Beschreibung der Möbel, gewünschtes Abholdatum
- **Bild-Upload** (bis zu 5 Fotos, je max. 5 MB)
- Echtzeit-Anzeige der verfügbaren Plätze für den gewählten Tag
- Datumssperre wenn der Tag voll ist

**Admin-Einstellungen pro Event:**
- Abholservice aktivieren/deaktivieren
- Frühestes / Spätestes Abholdatum (wird automatisch auf Event −8 bis −1 Tage vorgeschlagen)
- **Maximale Abholungen pro Tag** (z.B. 5): Wenn ein Tag ausgebucht ist, wird der Benutzer informiert und der Submit gesperrt
- Stornierung durch Admin gibt den Platz sofort wieder frei

**Admin-Anfragenverwaltung:**
- Alle eingegangenen Anfragen in einer Tabelle (sortierbar)
- Detail-Modal mit Fotos, Adresse, Admin-Notiz
- Status-Verwaltung: Ausstehend → Akzeptiert / Abgelehnt / Gelöscht
- Automatische Stornierungsmail bei Ablehnung

---

### Fahrzeugverwaltung

Verwaltung der Fahrzeuge, die für den Event eingesetzt werden.

**Fahrzeug-Stammdaten:**
- Fahrzeughalter (Name, Telefon, Kontakt/Bemerkung)
- Sortierbare Übersichtstabelle

**Fahrzeug-Zuweisungen:**
- Fahrzeug einem Event zuweisen
- Pro Einsatztag definierbar:
  - **Fahrer (Benutzer)**: Dropdown zeigt nur Benutzer, die sich als «Fahrer» für diesen Tag angemeldet haben
  - **Beifahrer (Benutzer)**: Dropdown zeigt nur Benutzer, die sich als «Beifahrer» für diesen Tag angemeldet haben
  - Fahrer Telefon wird automatisch aus dem Benutzerprofil übernommen
  - Manuelle Eingabe von Name/Telefon möglich (für externe Personen)
  - Bereit-ab-Zeit, Abgeholt-durch, Zurückgebracht-durch
- **Doppelbuchungsschutz**: Dieselbe Person kann pro Tag nur einem Fahrzeug zugeteilt werden
- Wenn ein Benutzer seine Fahrer/Beifahrer-Anmeldung storniert, wird er automatisch aus der Fahrzeugzuweisung entfernt
- Einsatztage: 8 Tage vor dem Event bis zum Eventtag

**Dashboard-Integration:**
- Benutzer sehen ihren Fahrzeugeinsatz direkt auf ihrer Anmeldungskarte

---

### Admin-Konfiguration

Zentrale Verwaltung unter `/admin/config`:

**Events:**
- CRUD für Events (Name, Datum, Beschreibung)
- Aktivieren / Deaktivieren

**Bereich-Vorlagen:**
- Stammdaten einmalig erfassen
- Sortierbar nach Name, Kategorie, Mindestalter, Standort

**Bereich-Zuweisungen:**
- Vorlage + Event + Datum/Zeit/Kapazität kombinieren
- Filter nach Event und Kategorie
- Sortierung aller Spalten
- **Bereich-Kopierfunktion**: Alle Zuweisungen von einem Event auf ein anderes kopieren (Datum wird automatisch angepasst)

**Kategorien:**
- Verwaltung der Bereichs-Kategorien (Sammeln, Sortieren, Verkauf, Fahrer, Sonstiges …)
- Löschen nur wenn keine Vorlagen verknüpft sind

---

### Admin-Anmeldeübersicht

Seite `/admin/appointments` mit Accordion-Ansicht:

- **Filter**: Event, Kategorie, Ansicht (Alle / Nur offene / Nur volle)
- **KPI-Chips**: Angemeldet, Offene Plätze, Bereiche, Abgesagt
- **Pro Bereich**: aufklappbarer Accordion-Eintrag mit:
  - Belegungsbalken (blau → orange → grün)
  - Badge: «⚠️ X offen», «✅ voll» oder «∞ Unbegrenzt»
  - Tabelle mit Name, Pfadiname, Kommentar, Anmeldedatum aller Angemeldeten
  - Abgesagte Anmeldungen (ausklappbar)
- Bereiche mit `MaxCapacity ≥ 999` werden als «Unbegrenzt» dargestellt

---

### Datenexport

Excel-Export (.xlsx) unter `/admin/export`:

- **Anmeldungen**: Alle Terminbuchungen gruppiert nach Kategorie, Bereich und Zeitslot mit Name, Pfadiname, Kommentar, Alter
- **Möbel-Abholungen**: Alle Anfragen mit Adresse, Status und Admin-Notiz
- **Benutzer**: Alle registrierten Konten mit Kontaktdaten
- **Familienmitglieder**: Alle hinterlegten Familienmitglieder
- **Event-Filter**: Alle Exporte können auf ein einzelnes Event eingeschränkt werden

---

## Rollen & Berechtigungen

| Bereich | Benutzer | Admin |
|---|---|---|
| Eigene Anmeldungen verwalten | ✅ | ✅ |
| Möbelabholung anmelden | ✅ | ✅ |
| Dashboard (Kalender, Fahrzeug) | ✅ | ✅ |
| Admin-Konfiguration | ❌ | ✅ |
| Anmeldeübersicht | ❌ | ✅ |
| Fahrzeugverwaltung | ❌ | ✅ |
| Möbel-Admin | ❌ | ✅ |
| Datenexport | ❌ | ✅ |

Der erste Admin-Benutzer wird beim Start automatisch angelegt (`admin@flomiapp.com` / `Admin123!`).

---

## Installation & Setup

### Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB reicht für Entwicklung)
- SMTP-Server (optional, für E-Mail-Benachrichtigungen)

### Schritte

```bash
# 1. Repository klonen
git clone https://github.com/danimerz/FlomiApp.git
cd FlomiApp

# 2. Datenbank migrieren (LocalDB wird automatisch angelegt)
dotnet ef database update

# 3. App starten
dotnet run
```

Die App startet standardmässig auf `https://localhost:5001`.

Beim ersten Start werden automatisch angelegt:
- Admin-Rolle
- Admin-Benutzer (`admin@flomiapp.com` / `Admin123!`)
- Basis-Bereichskategorien (Sammeln, Sortieren, Verkauf, Sonstiges)
- Standard-Möbelabholungs-Setting

---

## Konfiguration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FlomiApp;Trusted_Connection=True;"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "user@example.com",
    "Password": "secret",
    "FromAddress": "noreply@flomiapp.com",
    "FromName": "FlomiApp"
  }
}
```

> **Hinweis:** Für Produktion sollten Credentials über Umgebungsvariablen oder einen Secret Manager verwaltet werden.

---

## Lizenz

Dieses Projekt ist intern für die Pfadfinderabteilung entwickelt und nicht für die öffentliche Nutzung freigegeben.
