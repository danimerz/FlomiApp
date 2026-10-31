# FlomiApp

**FlomiApp** ist eine webbasierte Verwaltungsapplikation für den Flomi-Markt – einen Flohmarkt der Pfadfinderabteilung. Die App wurde mit **ASP.NET Core 10 Blazor Server** entwickelt und deckt die gesamte Organisation des Events ab: von der Benutzeranmeldung über die Bereichszuweisung bis hin zur Fahrzeugverwaltung, Möbelabholung und QR-Code Check-in.

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
  - [QR-Code Check-in](#qr-code-check-in)
- [Chat](#chat)
- [Datenexport](#datenexport)
- [Rollen & Berechtigungen](#rollen--berechtigungen)
- [Zwei-Faktor-Authentifizierung (2FA)](#zwei-faktor-authentifizierung-2fa)
- [Passkeys (Anmeldung ohne Passwort)](#passkeys-anmeldung-ohne-passwort)
- [Audit-Log](#audit-log)
- [Externe Anmeldung (Google, Microsoft, MiData)](#externe-anmeldung-google-microsoft-midata)
- [Installation & Setup](#installation--setup)
- [Konfiguration](#konfiguration)

---

## Technologie-Stack

| Komponente | Technologie |
|---|---|
| Framework | ASP.NET Core 10 Blazor Server (.NET 10) |
| Datenbank | MySQL 8 (Pomelo.EntityFrameworkCore.MySql) |
| ORM | Entity Framework Core 9 (bleibt auf 9, bis Pomelo EF Core 10 unterstützt) |
| Authentifizierung | ASP.NET Core Identity |
| E-Mail | SMTP via MailKit; asynchrone Mail-Queue als `IHostedService` (`Channel<T>`) |
| Verbindung | Eigene deutsche Anzeige bei Unterbruch (`#components-reconnect-modal` in `_Host.cshtml`, Ablauf in `wwwroot/js/reconnect.js`): erst Leiste oben, nach 8 s Karte mit Pfadi-Logo, Versuchszähler, Offline-Hinweis und «Neu laden»; danach kurz «✓ Wieder verbunden». Kennt der Server die Sitzung nicht mehr, lädt die Seite selbst neu. Nach einer Pause im Hintergrund prüft ein Ping die Verbindung; getrennte Sitzungen hält der Server 15 Min. |
| QR-Code | QRCoder (PNG-Generierung) + jsQR (Browser-Scan) |
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
- **Admin-Mails** an alle Konten mit Rolle `Admin` (einzeln abschaltbar unter *Einstellungen → Profil → E-Mail Einstellungen*):
  - **Neues Konto**: Name, Pfadiname, Stufe, Geburtstag mit Alter, E-Mail (bestätigt oder nicht), Telefon, Registrierungsweg (E-Mail und Passwort, Google, Microsoft, MiData), Zeitpunkt, Anzahl Konten total, Link zur Benutzerverwaltung
  - **Anmeldung / Stornierung für einen Bereich**: angemeldete Person (auch Familienmitglied) mit Stufe und Alter, Konto mit E-Mail und Telefon, Event, Bereich mit Kategorie, Datum, Zeit, Kommentar, Belegung des Zeitfensters («3 von 4 Plätzen, 1 frei»), Anzahl aktiver Anmeldungen des Kontos im Event, Zeitpunkt, Link zur Anmeldeübersicht
  - Empfänger: Rolle `Admin` mit bestätigter E-Mail und eingeschaltetem Schalter (Standard: an). Andere Admin-Rollen bekommen keine Admin-Mails. Ablauf: `AdminNotificationService` stellt den Inhalt zusammen, die Mail-Queue verschickt sie; ein Fehler dabei verhindert nie die Anmeldung
- **Passwort vergessen**: Reset-Link per E-Mail, gestylte Reset-Seite mit PW-Generator und Stärke-Indikator
- **Passkeys**: Anmeldung mit Face ID, Fingerabdruck oder Geräte-PIN statt Passwort, siehe [Passkeys](#passkeys-anmeldung-ohne-passwort)
- **Admin-Rollen**: Vollzugriff (`Admin`) oder auf einen Bereich beschränkte Rollen (`BereichsAdmin`, `FahrzeugAdmin`, `MoebelAdmin`), siehe [Rollen & Berechtigungen](#rollen--berechtigungen)
- **2FA-Pflicht** für alle Admin-Rollen (Authenticator-App oder Passkey), siehe [Zwei-Faktor-Authentifizierung](#zwei-faktor-authentifizierung-2fa)

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
- **Spontan** («Ich komme spontan vorbei»): Bereich ohne Datum und Zeit – eigene Gruppe zuoberst in der Anmeldung, keine Überschneidungsprüfung, pro Person nur einmal buchbar; Mails/Dashboard zeigen «spontan» statt Datum

**Anmeldung für Benutzer:**
- Bestätigungs-Modal mit optionalem Kommentar vor der Buchung
- Toggle-Switch für Halbtagsschicht (wenn konfiguriert, für freie Stufen)
- Kapazitätsprüfung, Altersminimum, Zeitkonflikt-Prüfung (gegen die tatsächlich gebuchte Schicht, bei Halbtag also die halbe), Pflicht-Stufen auch serverseitig
- **Klare Meldung, warum es nicht geht** (`AppointmentService.CheckRegistrationAsync` → `Helpers/RegistrationMessages`), mit Vorname bei Familienmitgliedern:
  - schon angemeldet → grün «✓ Du bist hier bereits angemeldet.» und Knopf «Im Dashboard ansehen»
  - Zeitüberschneidung → «Du bist zu dieser Zeit schon für «Fahrer» eingetragen (Sa 10.04., 08:00-12:00). Beides gleichzeitig geht nicht.»
  - zweiter Verkaufsbereich / zweiter Kuchen am Tag → nennt den bestehenden Bereich
  - voll → «Alle Plätze sind vergeben.», bzw. Hinweis auf die andere Schicht, wenn dort noch Platz ist und die Person den Halbtag buchen darf (nicht für Pflicht-Stufen)
  - Mindestalter → «Ab 16 Jahren – Lena ist 14.»
  - Bei Überschneidung, Verkauf und Kuchen zusätzlich der Link «Lieber wechseln? Im Dashboard stornieren»
- **Bestätigungs-E-Mail** mit Event, Bereich, Datum, Zeit und Kommentar nach erfolgreicher Anmeldung; enthält eingebetteten **QR-Code** wenn Check-in für das Event aktiviert ist (nur Kategorie Verkauf)
- **Erinnerung am Vortag** (`ReminderService`, Hintergrund-Job alle 15 Min.): am Vorabend um **18:00** (Schweizer Zeit) eine Mail pro Konto mit allen Einsätzen des nächsten Tages, auch für Familienmitglieder und Spontan-Bereiche. Pro Einsatz Bereich, Zeit, Standort, Kommentar; Fahrer und Beifahrer sehen ihr Fahrzeug und wer mitfährt; bei Verkauf mit Check-in nochmals der QR-Code. Link zum Dashboard aus `App:PublicUrl`. Wer «Bestätigungs-Mails» ausgeschaltet hat, bekommt keine. Wer sich erst nach 18:00 am Vorabend anmeldet, auch nicht (die Bestätigung kam ja gerade). `Appointment.ReminderSentAt` verhindert Doppelversand, auch nach einem Neustart. Pro Event abschaltbar («⏰ Erinnerung am Vortag» in der Event-Konfiguration, dort auch «Testmail an mich»); Beispiel-Events bekommen nie Erinnerungen

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

- **Startseite**: Neueste publizierte Meldung mit Datum, Titel, «Alle News»-Link und dem ersten Bild
- **Archivseite** `/news`: Alle publizierten Meldungen chronologisch, neueste zuerst mit blauem Highlight; Bilder als Galerie (ein Bild gross, mehrere als Vorschau, Klick öffnet volle Grösse)
- **Admin** `/admin/news`: CRUD für News-Einträge — Titel, Inhalt, bis 6 Bilder (verkleinert, ohne GPS-Daten), sofort publizieren oder als Entwurf speichern, Verbergen/Publizieren per Klick. Bilder von Entwürfen sind nicht öffentlich abrufbar

---

### Möbel-Abholservice

**Benutzerseite:**
- Auswahl des Events, Eingabe von Adresse, Möbel-Beschreibung, Abholdatum
- **Bild-Upload** (bis zu 5 Fotos, je max. 5 MB): Fotos werden beim Hochladen gedreht, auf max. 1600 px verkleinert (JPEG, Qualität 80) und von Metadaten inkl. GPS-Position befreit; dazu ein Vorschaubild mit 400 px
- Fotos werden 3 Monate nach dem Event automatisch gelöscht (Hintergrunddienst, täglich); ältere, noch unkomprimierte Fotos verkleinert derselbe Dienst nachträglich
- Echtzeit-Anzeige der verfügbaren Plätze; Datumssperre wenn Tag voll

**Admin-Einstellungen pro Event:**
- Abholservice aktivieren/deaktivieren
- Frühestes / Spätestes Abholdatum (automatisch Event −8 bis −1 Tage vorgeschlagen)
- **Maximale Abholungen pro Tag**: Kapazitätslimit pro Tag; stornierte Anfragen geben Platz frei
- «📅 Aus Event»-Button: Datumsbereich per Klick aus Event-Datum berechnen

**Admin-Anfragenverwaltung:**
- Sortierbare Tabelle, Detail-Modal mit Fotos und Admin-Notiz (Fotos über `/pickup-images/{id}`, nur für Möbel-Admins)
- Status: Ausstehend → Akzeptiert / Abgelehnt / Gelöscht

---

### Fahrzeugverwaltung

**Einsatzplan (Tab «Einsatzplan»):**
- Eine Tabelle pro Event: Zeilen = Fahrzeuge, Spalten = Einsatztage. Klick auf ein Feld trägt das Fahrzeug für den Tag ein bzw. ändert Fahrer und Helfer; «Bereit ab», «Abgeholt durch» und «Zurückgebracht durch» liegen unter «Mehr»
- Fahrer und Helfer in einem Feld: Name tippen – passt er zu einem Konto, wird es verknüpft (für den Tag Angemeldete zuerst vorgeschlagen), sonst bleibt es ein freier Name
- **🤖 Fahrer automatisch einteilen**: verteilt die für «Fahrer» resp. «Beifahrer» angemeldeten Personen auf Fahrzeuge ohne Besatzung (ganzes Event oder pro Tag); reichen die Beifahrer nicht, springen übrige Fahrer als Helfer ein; bestehende Einteilungen bleiben
- Tagesstatus im Spaltenkopf: rot = Abholungen, aber kein Fahrzeug mit Fahrer; gelb = Fahrzeug ohne Fahrer; grün = besetzt. Darunter die Liste «Angemeldet, aber noch nicht eingeteilt»
- **🔁 Fahrzeuge übernehmen**: Ist ein Event noch leer, werden die Fahrzeuge eines früheren Events mit denselben Tagen relativ zum Flomi übernommen (ohne Fahrer und Helfer). «🚀 Neues Jahr starten» macht das auf Wunsch gleich mit
- Neue Fahrzeuge direkt im Einsatzplan erfassen
- Telefonnummer wird automatisch aus dem Benutzerprofil übernommen
- **Doppelbuchungsschutz**: Eine Person kann pro Tag nur einem Fahrzeug zugeteilt werden
- Storniert ein Benutzer seinen Fahrer/Beifahrer-Bereich → automatische Entfernung aus der Fahrzeugzuweisung
- Einsatztage: 8 Tage vor dem Event bis zum Eventtag
- Benutzer sehen ihre Fahrzeugzuweisung direkt auf der Anmeldungskarte im Dashboard

**Routenplanung (Tab «Routenplanung»):**
- Pro Abholtag werden die akzeptierten Möbelabholungen auf die an diesem Tag zugewiesenen Fahrzeuge verteilt
- **🧭 Optimal verteilen**: Adressen werden über geo.admin.ch (Bund, gratis) in Koordinaten umgewandelt, die Fahrzeiten berechnet und die Stopps so verteilt und sortiert, dass Gesamtfahrzeit und längste Tour möglichst kurz sind (Startadresse → Stopps → zurück, 10 Min. Ladezeit pro Stopp). Fahrzeug und Stopp-Reihenfolge werden gespeichert
- **↕️ Nur Reihenfolge**: behält eine von Hand angepasste Zuteilung und optimiert nur die Reihenfolge pro Fahrzeug
- Fahrzeiten aus dem Strassennetz über OpenRouteService, falls `Routing__OpenRouteServiceApiKey` gesetzt ist (Gratiskonto auf openrouteservice.org); sonst Schätzung aus der Luftlinie
- Nicht gefundene Adressen werden gemeldet, markiert und nicht eingeplant
- «Route öffnen» (Google Maps) und Excel-Export folgen der geplanten Reihenfolge
- **🧪 Beispieldaten**: legt einen für Freiwillige unsichtbaren Beispiel-Event mit 3 Fahrzeugen und 19 Abholungen im Kanton Zug an (eine Adresse absichtlich falsch) und entfernt ihn auf Knopfdruck wieder. Beispiel-Mailadressen enden auf `.invalid`, dorthin werden keine Mails verschickt

---

### Admin-Konfiguration

Zentrale Verwaltung unter `/admin/config` mit 4 Tabs:

- **Events**: CRUD, Aktivieren/Deaktivieren; Ort und Öffnungszeiten des Markts. Das nächste aktive Event erscheint auf der Startseite und mit Ort bei Google als Veranstaltung (schema.org `Event`). «Neues Jahr starten» übernimmt den Ort und verschiebt die Öffnungszeiten mit.
- **Bereich-Vorlagen**: Stammdaten inkl. Pflicht-Stufen; Reihenfolge-Sortierung via ▲/▼
- **Bereich-Zuweisungen**: Vorlage + Event + Datum/Zeit/Kapazität + alternativer Zeitslot; Kopierfunktion zwischen Events
- **Kategorien**: Name, Reihenfolge frei sortierbar via ▲/▼

---

### Admin-Dashboard

Übersichtsseite `/admin/dashboard` mit zwei Tabs:

**Tab «Übersicht»:**
- 6 KPI-Kacheln: Anmeldungen, Offene Plätze, Offene Abholungen, Fahrzeuge, Benutzer, Stornierungen
- Hinweis auf kommende Tage, an denen Fahrer fehlen (Abholungen ohne Fahrzeug mit Fahrer, Fahrzeuge ohne Fahrer)
- «⏰ Erinnerung für morgen»: Anzahl Einsätze und Personen, vor 18:00 «geht heute um 18:00 raus», danach «verschickt an X von Y»
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
- Aktionen: Edit (Name/Mail/Stufe/Admin-Rollen), PW manuell setzen oder per 🔑 PW-Mail, 2FA zurücksetzen, 🔑 Passkeys entfernen, Sperren/Entsperren, Löschen
- Rollenänderungen, gesetzte Passwörter, versendete PW-Mails, 2FA-Resets, Sperrungen und Löschungen werden im [Audit-Log](#audit-log) protokolliert

---

### QR-Code Check-in

Admin-Seite `/admin/checkin` für die Einscheibung am Einsatztag (nur Kategorie **Verkauf**):

**Aktivierung:** Check-in wird pro Event ein-/ausgeschaltet. Nur aktive Events werden angezeigt.

**4-Tab-Interface:**
- **📷 QR-Scan**: Kamera öffnen und QR-Code von Dashboard oder E-Mail scannen; Modus-Toggle zwischen Check-in und Check-out
- **🔍 Manuell**: Suchfeld filtert alle Verkauf-Anmeldungen nach Name; Ein- und Auscheck-Buttons je nach aktuellem Status
- **👤 Walk-in**: Nicht registrierte Personen erfassen (Vorname, Nachname, Info); Liste der heutigen Walk-ins mit Auscheck-Funktion; Walk-ins werden in der Datenbank gespeichert
- **📋 Übersicht**: Kombinierte Tabelle aller Anmeldungen + Walk-ins mit Statusfilter (Offen / Eingecheckt / Ausgecheckt); farblich codiert

**Statistik-Karte** (immer sichtbar): Eingecheckt / Ausgecheckt / Ausstehend / Total-Zähler + Fortschrittsbalken + zuletzt aktive Personen.

**Dashboard-QR**: Verkauf-Teilnehmer sehen ihren persönlichen QR-Code direkt auf der Anmeldungskarte, solange noch nicht eingecheckt.

---

### Chat

Echtzeit-Chat unter `/admin/messages` (Admin-Rollen) und `/user/messages` (User):

- Direkte Nachrichten zwischen Admins und einzelnen Benutzern
- Echtzeit-Aktualisierung via Blazor-Events (kein Polling)
- Sidebar mit allen Konversationen, Ungelesene-Badge
- Admins können Nachrichten löschen

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

Neben normalen Benutzern gibt es vier Admin-Rollen. `Admin` hat Vollzugriff; die übrigen Rollen
geben nur Zugriff auf ihren Bereich. Einem Benutzer können mehrere Rollen zugewiesen werden
(unter `/admin/users` → Edit).

| Bereich | Benutzer | Admin | BereichsAdmin | FahrzeugAdmin | MoebelAdmin |
|---|---|---|---|---|---|
| Eigene Anmeldungen, Möbelabholung, Dashboard, News lesen | ✅ | ✅ | ✅ | ✅ | ✅ |
| Admin-Dashboard & Statistiken (`/admin/dashboard`) | ❌ | ✅ | ✅ | ✅ | ✅ |
| Check-in / Walk-in (`/admin/checkin`) | ❌ | ✅ | ✅ | ✅ | ✅ |
| Chat mit Benutzern (`/admin/messages`) | ❌ | ✅ | ✅ | ✅ | ✅ |
| Anmeldeübersicht + Stornierung (`/admin/appointments`) | ❌ | ✅ | ✅ | ❌ | ❌ |
| Fahrzeugverwaltung (`/admin/vehicles`) | ❌ | ✅ | ❌ | ✅ | ❌ |
| Möbel-Admin (`/admin/furniture-pickup`) | ❌ | ✅ | ❌ | ❌ | ✅ |
| Admin-Konfiguration (`/admin/config`) | ❌ | ✅ | ❌ | ❌ | ❌ |
| Benutzerverwaltung (`/admin/users`) | ❌ | ✅ | ❌ | ❌ | ❌ |
| News verwalten (`/admin/news`) | ❌ | ✅ | ❌ | ❌ | ❌ |
| Datenexport (`/admin/export`) | ❌ | ✅ | ❌ | ❌ | ❌ |
| Audit-Log (`/admin/auditlog`) | ❌ | ✅ | ❌ | ❌ | ❌ |

Alle Admin-Seiten verlangen zusätzlich zur Rolle eine aktivierte 2FA (siehe unten).

Beim Start legt die App die vier Rollen an, falls sie fehlen, und erstellt den ersten
Admin-Benutzer (`admin@flomiapp.com`) mit der Rolle `Admin`. Ist kein `AdminPassword` konfiguriert,
generiert die App ein zufälliges, starkes Passwort und zeigt es **einmalig** als Warnung in den
Server-Logs an; beim ersten Login muss es sofort geändert werden
(siehe [Installation & Setup](#installation--setup)).

---

## Zwei-Faktor-Authentifizierung (2FA)

Für alle Admin-Rollen ist 2FA **Pflicht**: entweder mit einer Authenticator-App (z.B. Google
Authenticator, Microsoft Authenticator, 1Password) oder durch die Anmeldung mit einem
[Passkey](#passkeys-anmeldung-ohne-passwort). Normale Benutzer brauchen keine 2FA.

- **Einrichtung beim Login:** Meldet sich jemand mit Admin-Rolle ohne aktive 2FA an, leitet die App
  direkt auf die Einrichtungsseite weiter. Dort erscheint ein QR-Code (alternativ der Schlüssel zum
  Abtippen); nach Eingabe eines gültigen Codes ist 2FA aktiv.
- **Wiederherstellungscodes:** Nach der Aktivierung werden einmalig 10 Recovery-Codes angezeigt.
  Sie ersetzen beim Login den Authenticator-Code, falls das Gerät verloren geht – sicher aufbewahren.
- **Durchsetzung:** Die Policy `RequireTwoFactorForAdmins` prüft auf jeder Admin-Seite, ob 2FA aktiv
  ist. Erfüllt ist sie, wenn die Authenticator-App eingerichtet ist (Claim `tfa_enrolled`) oder
  die aktuelle Sitzung mit einem Passkey eröffnet wurde (Claim `amr=passkey`). Ohne 2FA bleiben die
  Admin-Seiten gesperrt, auch wenn die Rolle vergeben ist.
- **Zurücksetzen:** Ein `Admin` kann die 2FA eines Benutzers unter `/admin/users` zurücksetzen
  (z.B. bei Gerätewechsel ohne Recovery-Codes). Die Person richtet 2FA beim nächsten Login neu ein.
- **Reihenfolge beim ersten Login:** Ein automatisch generiertes Passwort muss zuerst geändert
  werden, danach folgt die 2FA-Einrichtung.

---

## Passkeys (Anmeldung ohne Passwort)

Ein Passkey ersetzt Passwort und Code: Man meldet sich mit Face ID, Fingerabdruck oder der
Geräte-PIN an. iPhone/Mac speichern Passkeys im iCloud-Schlüsselbund, Android/Chrome im
Google-Passwortmanager, Windows in Windows Hello – über diese Dienste sind sie auf allen eigenen
Geräten verfügbar. Passkeys sind freiwillig; Passwort und «Passwort vergessen» funktionieren weiterhin.

**Für Benutzer**

- **Einrichten:** `/user/settings` → Tab «🔒 Sicherheit» → Karte «🔑 Passkeys» → Namen vergeben
  (Vorschlag aus dem Gerät, z.B. «iPhone») → «Passkey hinzufügen» → am Gerät bestätigen.
  Bis zu 10 Passkeys pro Konto (mehrere Geräte, Sicherheitsschlüssel).
- **Anmelden:** Login-Seite → «🔑 Mit Passkey anmelden». Auf neueren Geräten schlägt schon das
  E-Mail-Feld den Passkey vor (Autofill). «Angemeldet bleiben» gilt auch für Passkeys.
- **Verwalten:** In derselben Karte umbenennen oder entfernen; angezeigt werden «hinzugefügt» und
  «zuletzt verwendet». Ein entfernter Passkey sollte auch im Passwortmanager des Geräts gelöscht werden.
- **Hinweis im Dashboard:** Wer noch keinen Passkey hat und einen passenden Browser nutzt, sieht
  im Dashboard einmal «Nächstes Mal ohne Passwort?». «Nicht mehr anzeigen» merkt sich das Gerät
  (`localStorage` `flomi.passkeyHint.dismissed`).

**Für Admins**

- Eine Anmeldung mit Passkey **erfüllt die 2FA-Pflicht** – kein Authenticator-Code nötig. Der Passkey
  prüft selbst zwei Faktoren: Besitz des Geräts und Face ID/Fingerabdruck/PIN (User Verification
  ist Pflicht). Er ist zudem phishing-sicher, weil er nur auf `pfadiflomi.ch` funktioniert.
- Admins **ohne** Authenticator-App, die sich mit Passwort anmelden, landen wie bisher auf der
  2FA-Einrichtung. Dort steht der Hinweis, alternativ einen Passkey einzurichten bzw. sich mit dem
  vorhandenen Passkey neu anzumelden.
- Mit Passwort angemeldete Admins kommen nur mit eingerichteter Authenticator-App auf die Admin-Seiten
  (wie bisher). Ein vorhandener Passkey allein genügt dafür nicht – entscheidend ist, wie die
  *aktuelle Sitzung* eröffnet wurde.
- **Gerät verloren:** Unter `/admin/users` → «🔑 Passkeys (n)» entfernt ein `Admin` alle Passkeys einer
  Person; offene Sitzungen werden dabei beendet (Security-Stamp). Wird im Audit-Log protokolliert.

**Technik**

- Die WebAuthn-Prüfung (Challenge, Herkunft, Signatur, Signaturzähler) übernimmt ASP.NET Core
  Identity 10 (`SignInManager.MakePasskeyCreationOptionsAsync`, `PerformPasskeyAttestationAsync`,
  `MakePasskeyRequestOptionsAsync`, `PerformPasskeyAssertionAsync`). Die Challenge liegt dabei
  kurzzeitig in einem Identity-Cookie.
- **Speicherung:** Identity 10 bringt die Passkey-Tabelle erst mit `Identity.EntityFrameworkCore` 10
  (EF Core 10) – die App bleibt wegen Pomelo auf EF Core 9. Deshalb speichert der eigene
  `Security/ApplicationUserStore` (erbt vom EF-UserStore, implementiert `IUserPasskeyStore`) in die
  Tabelle `UserPasskeys` (Migration `AddUserPasskeys`). Gespeichert wird nur der öffentliche
  Schlüssel samt Metadaten, nichts Geheimes.
- **Abläufe:** `wwwroot/js/passkey.js` ruft `navigator.credentials` auf. Endpunkte in
  `Endpoints/PasskeyEndpoints.cs`: `POST /passkeys/creation-options` und `POST /passkeys/register`
  (angemeldet), `POST /passkeys/request-options` (anonym). Die Anmeldung selbst ist der Handler
  `Passkey` der Login-Seite. Die Einstellungen (Blazor) rufen `flomiPasskeys.register()` per JS-Interop
  auf, weil das Erstellen den HttpContext braucht.
- **Sitzungsmerkmal:** Nach der Passkey-Anmeldung trägt das Login-Cookie den Claim `amr=passkey`.
  `SecurityStampValidatorOptions.OnRefreshingPrincipal` übernimmt ihn bei der periodischen
  Prüfung (alle 30 Min.), sonst verlören Admins mitten in der Sitzung den Zugang.
- **Domain:** Passkeys sind an die Domain gebunden (Relying Party ID = Host der Anfrage, also
  `pfadiflomi.ch`; `www.` wird vorher umgeleitet). Nach einem Domainwechsel funktionieren bestehende
  Passkeys nicht mehr – das Passwort schon. Gibt ein Reverse Proxy einen anderen Host weiter, die
  Domain fest setzen: `Passkeys__ServerDomain=pfadiflomi.ch`. Lokal (`localhost`) funktionieren
  Passkeys ebenfalls.
- **Später mit EF Core 10:** `Identity.EntityFrameworkCore` 10 nutzen (Identity-Schema-Version 3,
  Tabelle `AspNetUserPasskeys`), Daten aus `UserPasskeys` übernehmen, `ApplicationUserStore`
  entfernen. Die Abläufe und das JavaScript bleiben gleich.

---

## Audit-Log

Unter `/admin/auditlog` (nur Rolle `Admin`) wird protokolliert, welcher Admin wann was geändert hat.
Jeder Eintrag enthält Zeitpunkt, Admin, Aktion, betroffene Person und Details
(z.B. «Rolle 'Admin' entfernt»).

| Aktion | Auslöser |
|---|---|
| Benutzer gelöscht | `/admin/users` |
| Benutzer gesperrt / entsperrt | `/admin/users` |
| Rolle geändert | `/admin/users` (pro hinzugefügter/entfernter Rolle) |
| Passwort gesetzt | `/admin/users` (manuell gesetztes Passwort) |
| PW-Reset-Mail gesendet | `/admin/users` (Reset-Link per Mail) |
| 2FA zurückgesetzt | `/admin/users` |
| Passkeys zurückgesetzt | `/admin/users` (alle Passkeys einer Person entfernt) |
| Passkey hinzugefügt / entfernt | `/user/settings` (die Person selbst; Name des Passkeys in den Details) |
| Anmeldung storniert (Admin) | `/admin/appointments` |

Die Übersicht lässt sich nach Admin-Name, Aktion und Zeitraum filtern und zeigt die neuesten
300 Einträge.

---

## Externe Anmeldung (Google, Microsoft, MiData)

Benutzer können sich zusätzlich zu E-Mail/Passwort mit **Google**, **Microsoft** oder **MiData**
(Mitgliederdatenbank der Pfadibewegung Schweiz, `db.scout.ch`) anmelden. Ein Anbieter erscheint nur,
wenn er konfiguriert ist; ohne Konfiguration bleibt alles beim Passwort-Login.

**Ablauf für Benutzer:**
- **Erste Anmeldung:** Name, E-Mail und (bei MiData) Pfadiname und Geburtstag werden übernommen,
  Stufe und Telefonnummer ergänzt die Person auf «Profil vervollständigen».
- **Bestehendes Konto mit gleicher E-Mail:** Wird automatisch verknüpft, wenn der Anbieter die
  E-Mail als bestätigt meldet (`TrustEmail`, siehe unten). Sonst meldet sich die Person mit Passwort
  an und verknüpft unter *Einstellungen → Sicherheit → Verknüpfte Konten*.
- **Unbestätigte E-Mail** (oder im Formular geändert): Es geht wie bei der normalen Registrierung
  ein Bestätigungslink raus.
- **Konten ohne Passwort** können unter *Einstellungen → Sicherheit* eines festlegen und bestätigen
  das Löschen des Kontos mit ihrer E-Mail-Adresse.
- **Admin-Konten** (alle vier Admin-Rollen) können sich nicht extern anmelden und nichts verknüpfen –
  für sie gelten Passwort und 2FA.

### Konfiguration

Auf dem Server als Umgebungsvariablen (doppelter Unterstrich = Abschnitt), lokal per User Secrets:

```bash
ExternalAuth__Google__ClientId=...
ExternalAuth__Google__ClientSecret=...
ExternalAuth__Microsoft__ClientId=...
ExternalAuth__Microsoft__ClientSecret=...
ExternalAuth__MiData__Authority=https://db.scout.ch      # Test: https://pbs.puzzle.ch
ExternalAuth__MiData__ClientId=...
ExternalAuth__MiData__ClientSecret=...
```

`TrustEmail` (Standard in `appsettings.json`: Google `true`, Microsoft `false`, MiData `false`) legt
fest, ob die E-Mail des Anbieters als bestätigt gilt. Bei Microsoft ist sie bei Geschäftskonten nicht
zuverlässig verifiziert; bei MiData nach Rücksprache mit der PBS entscheiden.

**Callback-URLs**, die bei den Anbietern eingetragen werden müssen (öffentliche Adresse der App: `https://pfadiflomi.ch`):

| Anbieter | Callback-URL |
|---|---|
| Google | `https://pfadiflomi.ch/signin-google` |
| Microsoft | `https://pfadiflomi.ch/signin-microsoft` |
| MiData | `https://pfadiflomi.ch/signin-midata` |

Die Adresse muss exakt stimmen: `www.pfadiflomi.ch` ist für die Anbieter eine andere Adresse als
`pfadiflomi.ch`. Die App leitet `www.…` selbst per 301 auf die Adresse ohne `www` um (`AddRedirectToNonWwwPermanent`
in `Program.cs`), deshalb reicht die eine URL.

Die App muss hinter dem Reverse Proxy das `https`-Schema erkennen (`X-Forwarded-Proto`), sonst lehnt der
Anbieter mit `redirect_uri_mismatch` ab. Läuft der Proxy nicht auf demselben Host, seine IP unter
`ForwardedHeaders__KnownProxies__0=<IP>` eintragen.

### Google einrichten

1. In der [Google Cloud Console](https://console.cloud.google.com/) ein Projekt anlegen (z.B. «FlomiApp»).
2. *APIs & Dienste → OAuth-Zustimmungsbildschirm*: Typ **Extern**, App-Name, Support-E-Mail, Logo
   optional. Bereiche: `openid`, `email`, `profile`. Autorisierte Domain `pfadiflomi.ch`,
   Datenschutzerklärung `https://pfadiflomi.ch/datenschutz`.
3. *Anmeldedaten → Anmeldedaten erstellen → OAuth-Client-ID*, Typ **Webanwendung**, autorisierte
   Weiterleitungs-URI `https://pfadiflomi.ch/signin-google`.
4. Client-ID und Clientschlüssel in die Konfiguration übernehmen.
5. Den Zustimmungsbildschirm auf **In Produktion** stellen, sonst können sich nur eingetragene
   Testbenutzer anmelden.

### Microsoft einrichten

1. Im [Microsoft Entra Admin Center](https://entra.microsoft.com/) anmelden. App-Registrierungen
   brauchen ein Entra-Verzeichnis (Mandant); wer keins hat, legt es z.B. über ein kostenloses
   Azure-Konto an.
2. *Anwendungen → App-Registrierungen → Neue Registrierung*, unterstützte Kontotypen:
   **Konten in einem beliebigen Organisationsverzeichnis und persönliche Microsoft-Konten**.
3. Umleitungs-URI: Plattform **Web**, `https://pfadiflomi.ch/signin-microsoft`.
4. *Zertifikate & Geheimnisse → Neuer geheimer Clientschlüssel*. Den **Wert** (nicht die ID) als
   `ClientSecret` übernehmen, die *Anwendungs-ID (Client)* als `ClientId`.
5. Der Schlüssel läuft nach höchstens 24 Monaten ab – Ablaufdatum notieren und rechtzeitig erneuern.

### MiData einrichten

MiData ist ein OpenID-Connect-Anbieter (hitobito). Die Anwendung muss vom IT-Support der PBS registriert
werden: [Antragsformular](https://forms.cloud.microsoft/e/926Aw4Seh0),
[Dokumentation](https://docu.scout.ch/de/documentation/article-15).

- Im Antrag angeben: Callback-URL `https://pfadiflomi.ch/signin-midata`, Scopes `openid email name with_roles`,
  zuerst für die **Integrationsumgebung** (`https://pbs.puzzle.ch`), danach für Produktion.
- Technische Details: Endpunkte kommen aus `https://db.scout.ch/.well-known/openid-configuration`.
  MiData unterstützt PKCE nur im Modus `plain`, deshalb ist PKCE für MiData abgeschaltet (vertraulicher
  Client mit Secret).
- Über `with_roles` liefert MiData auch die Rollen und Gruppen der Person. Die App wertet sie noch nicht
  aus; später liesse sich damit z.B. die Stufe vorschlagen oder der Zugang auf die eigene Abteilung
  beschränken.

---

## Installation & Setup

### Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- MySQL 8.0+ Server (lokal oder gehostet)
- SMTP-Server (optional, für E-Mail-Benachrichtigungen)

### Schritte

```bash
# 1. Repository klonen
git clone https://github.com/danimerz/FlomiApp.git
cd FlomiApp

# 2. MySQL-Verbindungsstring setzen (User Secrets empfohlen)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Port=3306;Database=FlomiApp;User Id=root;Password=secret;CharSet=utf8mb4;"

# 3. Admin-Passwort setzen (optional — ohne das wird eines generiert, siehe unten)
dotnet user-secrets set "AdminPassword" "DeinSicheresPasswort!"

# 4. Datenbank migrieren
dotnet ef database update

# 5. App starten
dotnet run
```

Die App startet standardmässig auf `https://localhost:5001`.

Beim ersten Start werden automatisch angelegt:
- Admin-Rolle und Admin-Benutzer (`admin@flomiapp.com`)
- Basis-Bereichskategorien (Sammeln, Sortieren, Verkauf, Sonstiges)
- Standard-Möbelabholungs-Setting

**Admin-Passwort beim ersten Start:**
- Wurde `AdminPassword` gesetzt (Schritt 3), wird genau dieses Passwort verwendet.
- Wurde nichts gesetzt, generiert die App ein zufälliges 16-stelliges Passwort und schreibt es
  **einmalig** als Warnung in die Konsolen-Logs des ersten Starts (danach nirgends mehr abrufbar).
  In beiden Fällen gilt: Ist das Passwort automatisch generiert worden, verlangt die App beim
  ersten Login eine sofortige Änderung, bevor das Konto nutzbar ist.

---

## Tests

Automatisierte Tests (xUnit) decken die zentrale Buchungslogik ab — Kapazitäts-, Alters- und
Zeitkonflikt-Prüfung, Verkauf-/Kuchen-Sonderregeln, Stornierung inkl. Fahrzeugzuweisung, sowie die
reinen Helper (Telefonnummer-/Zeit-Formatierung, Bild-Signatur-Erkennung, Foto-Aufbereitung) und die
Löschfrist der Möbelfotos, Passkeys und die Admin-Mails (Empfänger, Inhalt, HTML-Kodierung). Sie laufen gegen eine
SQLite-In-Memory-Datenbank, keine echte MySQL-Instanz nötig:

```bash
dotnet test FlomiApp.sln
```

Der Deploy (`.github/workflows/deploy_famMerz.yaml`) läuft bei jedem Push auf `master`
(reine `.md`-Änderungen ausgenommen) oder manuell. Er führt `dotnet test` vor dem Publish aus —
ein fehlschlagender Test stoppt den Deploy. Die Version `1.0.<Lauf-Nummer> (<Commit>)` steht im Footer.

---

## Konfiguration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=FlomiApp;User Id=root;Password=...;CharSet=utf8mb4;"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 465,
    "Username": "user@example.com",
    "Password": "secret",
    "FromAddress": "noreply@flomiapp.com",
    "FromName": "FlomiApp"
  }
}
```

> **Sicherheit:** Connection-String und SMTP-Credentials gehören **nicht** in `appsettings.json`. Für Entwicklung `.NET User Secrets` verwenden (`dotnet user-secrets set ...`), für Produktion Umgebungsvariablen oder einen Secret Manager.

Optional: `Passkeys:ServerDomain` (Umgebungsvariable `Passkeys__ServerDomain`) legt die Domain der
Passkeys fest. Leer = Host der Anfrage; nur nötig, wenn der Reverse Proxy einen anderen Host
weitergibt. Siehe [Passkeys](#passkeys-anmeldung-ohne-passwort).

### Suchmaschinen (SEO)

- `/robots.txt` und `/sitemap.xml` erzeugt die App selbst (`Endpoints/SeoEndpoints.cs`) mit der aufgerufenen
  Domain. Admin- und Benutzerseiten sind für Suchmaschinen gesperrt.
- Öffentliche Seiten (Start, News, Möbel-Abholservice, Login, Registrierung, Impressum, Datenschutz) haben
  Titel, Beschreibung, Canonical-Link und Open-Graph-Vorschau (Bild und Text beim Teilen, z. B. in WhatsApp)
  über die Komponente `Shared/Seo.razor`. Texte und Seitenliste stehen in `Helpers/SeoHelper.cs`.
- Die Startseite liefert strukturierte Daten: schema.org `WebSite` (Seitenname «Pfadi Flomi») und, sobald beim
  nächsten Event ein Ort hinterlegt ist, `Event` (Datum, Öffnungszeiten, Ort; `Helpers/MarketEventHelper.cs`).
- Nach dem Go-live die Domain in der [Google Search Console](https://search.google.com/search-console)
  bestätigen und `https://pfadiflomi.ch/sitemap.xml` einreichen.

### Design (Theme)

Das Erscheinungsbild gilt für die ganze Installation und wird über `Design:Theme` gewählt, auf dem Server am einfachsten als Umgebungsvariable:

```bash
Design__Theme=morgarten
```

| Wert | Aussehen |
|---|---|
| `standard` | Blaues FlomiApp-Design (Vorgabe, auch bei leerem oder unbekanntem Wert) |
| `morgarten` | Morgarten Navy: Farben aus dem Logo der Pfadi Morgarten, Navy-Seitenleiste mit Blau-Rot-Streifen, Exo 2 / Open Sans |
| `morgarten-lager` | Nah an pfadimorgarten.ch: schwarze Leiste, warmgrauer Grund, Seitentitel in Amatic SC |

Hell- und Dunkelmodus der Benutzer funktionieren in jedem Theme. Eine Änderung wirkt nach einem Neustart der App. Die Schriften liegen unter `wwwroot/fonts` und werden nicht von Google geladen.

Für Entwickler: Farben in CSS nie fest eintragen, sondern über die Marken-Tokens `--brand-50` … `--brand-800` (bzw. `rgba(var(--brand-600-rgb), …)`) und die `--color-*`-Tokens aus `site.css`. Ein Theme ist eine Datei `wwwroot/css/themes/<name>.css` plus ein Eintrag in `Helpers/DesignTheme.cs`.

---

## Lizenz

Dieses Projekt ist intern für die Pfadfinderabteilung entwickelt und nicht für die öffentliche Nutzung freigegeben.
