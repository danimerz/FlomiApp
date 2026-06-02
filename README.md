# FlomiApp

**FlomiApp** ist eine webbasierte Verwaltungsapplikation für den Flomi-Markt – einen Flohmarkt der Pfadfinderabteilung. Die App wurde mit **ASP.NET Core 8 Blazor Server** entwickelt und deckt die gesamte Organisation des Events ab: von der Benutzeranmeldung über die Bereichszuweisung bis hin zur Fahrzeugverwaltung und Möbelabholung.

---

## Inhaltsverzeichnis

- [Technologie-Stack](#technologie-stack)
- [Funktionsübersicht](#funktionsübersicht)
  - [Benutzerverwaltung](#benutzerverwaltung)
  - [Anmeldung zu Bereichen](#anmeldung-zu-bereichen)
  - [Benutzerdashboard](#benutzerdashboard)
  - [News & Mitteilungen](#news--mitteilungen)
  - [Möbel-Abholservice](#möbel-abholservice)
  - [Fahrzeugverwaltung](#fahrzeugverwaltung)
  - [Admin-Konfiguration](#admin-konfiguration)
  - [Admin-Dashboard](#admin-dashboard)
  - [Admin-Anmeldeübersicht](#admin-anmeldeübersicht)
  - [Admin-Benutzerverwaltung](#admin-benutzerverwaltung)
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
| Diagramme | Chart.js 4 (via CDN + JS Interop) |
| UI | Custom CSS mit Design-Token-System (Light/Dark Mode) |

---

## Funktionsübersicht

### Benutzerverwaltung

- **Registrierung & Login** über ASP.NET Identity mit Live-Passwort-Stärke-Anzeige und Anforderungs-Checkliste
- **Benutzerprofil** mit Vorname, Nachname, Pfadiname, Stufe, Geburtstag, Telefon, E-Mail
- **Familienmitglieder** können pro Konto hinterlegt werden (eigene Anmeldungen für Kinder / Begleitpersonen)
- **E-Mail-Benachrichtigungen** können pro Benutzer aktiviert/deaktiviert werden
- **Passwort vergessen**: Reset-Link per E-Mail, gestylte Reset-Seite mit PW-Generator und Stärke-Indikator
- **Admin-Rolle**: Erweiterte Verwaltungsrechte für ausgewählte Benutzer

---

### Anmeldung zu Bereichen

Benutzer können sich für **Einsatzbereiche** des Flomi-Events anmelden.

**Bereichs-Stammdaten (Vorlagen)** werden einmalig erfasst und enthalten:
- Name (z.B. «Geschirr», «Spielzeug», «Fahrer»)
- Kategorie (z.B. Verkauf, Sammeln, Sortieren, Fahrer) — Reihenfolge der Kategorien frei konfigurierbar
- Mindestalter, Standort
- **Pflicht-Stufen**: Stufen die zwingend den Haupt-Zeitslot buchen müssen (z.B. «Pfadi,Pio,Leiter»)

**Bereichs-Zuweisungen** verknüpfen eine Vorlage mit einem Event und definieren:
- Datum, Zeitslot, Maximale Kapazität (oder «Unbegrenzt»)
- **Alternativer Zeitslot** (z.B. Halbtagsschicht): Kapazität und Zeit separat konfigurierbar

**Anmeldung für Benutzer:**
- Bestätigungs-Modal mit optionalem Kommentar vor der Buchung
- Toggle-Switch für Halbtagsschicht (wenn konfiguriert, für freie Stufen)
- Kapazitätsprüfung, Altersminimum, Zeitkonflikt-Prüfung
- **Bestätigungs-E-Mail** mit Event, Bereich, Datum, Zeit und Kommentar nach erfolgreicher Anmeldung

**Admin-Hilfsfunktionen:**
- Bereiche können zwischen Events **kopiert** werden (inkl. alternativer Zeitslot)
- Filter, Sortierung und Event-Dropdown in der Übersicht

---

### Benutzerdashboard

- **Alle aktiven Anmeldungen** gruppiert nach Event und Datum (aufklappbar)
- **Kalenderansicht** (Toggle): Monatskalender mit Terminen und Möbelabholungen als farbige Chips
- **Fahrzeugzuweisung**: Fahrzeughalter-Name direkt auf der Anmeldungskarte sichtbar
- **Möbel-Abholungsanfragen** mit Status und Admin-Notiz
- **Absagen** von Anmeldungen mit automatischer Stornierungsmail
- **Kalender-Download** (.ics) für jeden Termin

---

### News & Mitteilungen

- **Startseite**: Neueste publizierte Meldung mit Datum, Titel und «Alle News»-Link
- **Archivseite** `/news`: Alle publizierten Meldungen chronologisch, neueste zuerst mit blauem Highlight
- **Admin** `/admin/news`: CRUD für News-Einträge — Titel, Inhalt, sofort publizieren oder als Entwurf speichern, Verbergen/Publizieren per Klick

---

### Möbel-Abholservice

**Benutzerseite:**
- Auswahl des Events, Eingabe von Adresse, Möbel-Beschreibung, Abholdatum
- **Bild-Upload** (bis zu 5 Fotos, je max. 5 MB)
- Echtzeit-Anzeige der verfügbaren Plätze; Datumssperre wenn Tag voll

**Admin-Einstellungen pro Event:**
- Abholservice aktivieren/deaktivieren
- Frühestes / Spätestes Abholdatum (automatisch Event −8 bis −1 Tage vorgeschlagen)
- **Maximale Abholungen pro Tag**: Kapazitätslimit pro Tag; stornierte Anfragen geben Platz frei
- «📅 Aus Event»-Button: Datumsbereich per Klick aus Event-Datum berechnen

**Admin-Anfragenverwaltung:**
- Sortierbare Tabelle, Detail-Modal mit Fotos und Admin-Notiz
- Status: Ausstehend → Akzeptiert / Abgelehnt / Gelöscht

---

### Fahrzeugverwaltung

**Fahrzeug-Zuweisungen:**
- Pro Einsatztag: Fahrer + Beifahrer aus gefilterten Dropdowns (nur Personen, die sich für «Fahrer» resp. «Beifahrer»-Bereiche am jeweiligen Tag angemeldet haben)
- Telefonnummer wird automatisch aus dem Benutzerprofil übernommen
- **Doppelbuchungsschutz**: Eine Person kann pro Tag nur einem Fahrzeug zugeteilt werden
- Storniert ein Benutzer seinen Fahrer/Beifahrer-Bereich → automatische Entfernung aus der Fahrzeugzuweisung
- Einsatztage: 8 Tage vor dem Event bis zum Eventtag
- Benutzer sehen ihre Fahrzeugzuweisung direkt auf der Anmeldungskarte im Dashboard

---

### Admin-Konfiguration

Zentrale Verwaltung unter `/admin/config` mit 4 Tabs:

- **Events**: CRUD, Aktivieren/Deaktivieren
- **Bereich-Vorlagen**: Stammdaten inkl. Pflicht-Stufen; Reihenfolge-Sortierung via ▲/▼
- **Bereich-Zuweisungen**: Vorlage + Event + Datum/Zeit/Kapazität + alternativer Zeitslot; Kopierfunktion zwischen Events
- **Kategorien**: Name, Reihenfolge frei sortierbar via ▲/▼

---

### Admin-Dashboard

Übersichtsseite `/admin/dashboard` mit zwei Tabs:

**Tab «Übersicht»:**
- 6 KPI-Kacheln: Anmeldungen, Offene Plätze, Offene Abholungen, Fahrzeuge, Benutzer, Stornierungen
- Bereichs-Auslastungsbalken (Top 8)
- Aktivitäts-Feed: Chronologische Timeline (Heute/Gestern/Datum) aller Anmeldungen, Stornierungen und Möbelanfragen
- Neueste ausstehende Möbelanfragen

**Tab «Statistiken»:**
- 4 interaktive Chart.js-Diagramme: Anmeldungen über Zeit (Line), Nach Kategorie (Donut), Nach Stufe (Bar), Belegungsrate pro Bereich (Horizontal Bar)
- Event-Filter oben, KPI-Zusammenfassung

---

### Admin-Anmeldeübersicht

Seite `/admin/appointments`:

- Filter: Event, Kategorie, Ansicht (Alle / Nur offene / Nur volle) + «Alle öffnen/schliessen»-Button
- Accordion pro Bereich/Zeitslot: Halbtagsschichten erscheinen als separate Einträge
- Pro Person: 🗑️-Button zur Admin-Stornierung direkt in der Tabelle
- Belegungsbalken, Badges, Abgesagte ausklappbar

---

### Admin-Benutzerverwaltung

Seite `/admin/users`:

- Tabelle mit allen Benutzern, Suche, Filter nach Stufe, sortierbare Spalten
- Tabs: Alle / Admins / Gesperrt
- **Anmeldungszähler**: Offene Anmeldungen pro Benutzer direkt in der Tabelle
- Aktionen: Edit (Name/Mail/Stufe/Admin-Rolle), PW manuell setzen oder per 🔑 PW-Mail, Sperren/Entsperren, Löschen

---

### Datenexport

Excel-Export (.xlsx) unter `/admin/export`:

- **Anmeldungen Verkauf**: Kategorie «Verkauf», gruppiert nach Bereich — Ganz- und Halbtagsschichten mit Zeitslot-Spalte
- **Anmeldungen Gastro**: Kategorie «Gastro», gleiche Struktur
- **Fahrzeug-Einsätze**: Pro Datum eine Sektion mit Fahrzeughalter, Fahrer, Beifahrer, Bereit-ab, Abgeholt durch, Zurückgebracht durch
- **Möbel-Abholungen**: Alle Anfragen mit Adresse, Status, Admin-Notiz
- **Event-Filter**: Alle Exporte auf ein einzelnes Event einschränkbar

---

## Rollen & Berechtigungen

| Bereich | Benutzer | Admin |
|---|---|---|
| Eigene Anmeldungen verwalten | ✅ | ✅ |
| Möbelabholung anmelden | ✅ | ✅ |
| Dashboard (Kalender, Fahrzeug) | ✅ | ✅ |
| News lesen | ✅ | ✅ |
| Admin-Dashboard & Statistiken | ❌ | ✅ |
| Admin-Konfiguration | ❌ | ✅ |
| Anmeldeübersicht + Stornierung | ❌ | ✅ |
| Benutzerverwaltung | ❌ | ✅ |
| Fahrzeugverwaltung | ❌ | ✅ |
| Möbel-Admin | ❌ | ✅ |
| News verwalten | ❌ | ✅ |
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
- Admin-Rolle und Admin-Benutzer (`admin@flomiapp.com` / `Admin123!`)
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
