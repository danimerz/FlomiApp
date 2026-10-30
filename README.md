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
- **Passwort vergessen**: Reset-Link per E-Mail, gestylte Reset-Seite mit PW-Generator und Stärke-Indikator
- **Admin-Rollen**: Vollzugriff (`Admin`) oder auf einen Bereich beschränkte Rollen (`BereichsAdmin`, `FahrzeugAdmin`, `MoebelAdmin`), siehe [Rollen & Berechtigungen](#rollen--berechtigungen)
- **2FA-Pflicht** für alle Admin-Rollen (Authenticator-App), siehe [Zwei-Faktor-Authentifizierung](#zwei-faktor-authentifizierung-2fa)

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
- Kapazitätsprüfung, Altersminimum, Zeitkonflikt-Prüfung
- **Bestätigungs-E-Mail** mit Event, Bereich, Datum, Zeit und Kommentar nach erfolgreicher Anmeldung; enthält eingebetteten **QR-Code** wenn Check-in für das Event aktiviert ist (nur Kategorie Verkauf)

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

- **Events**: CRUD, Aktivieren/Deaktivieren
- **Bereich-Vorlagen**: Stammdaten inkl. Pflicht-Stufen; Reihenfolge-Sortierung via ▲/▼
- **Bereich-Zuweisungen**: Vorlage + Event + Datum/Zeit/Kapazität + alternativer Zeitslot; Kopierfunktion zwischen Events
- **Kategorien**: Name, Reihenfolge frei sortierbar via ▲/▼

---

### Admin-Dashboard

Übersichtsseite `/admin/dashboard` mit zwei Tabs:

**Tab «Übersicht»:**
- 6 KPI-Kacheln: Anmeldungen, Offene Plätze, Offene Abholungen, Fahrzeuge, Benutzer, Stornierungen
- Hinweis auf kommende Tage, an denen Fahrer fehlen (Abholungen ohne Fahrzeug mit Fahrer, Fahrzeuge ohne Fahrer)
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
- Aktionen: Edit (Name/Mail/Stufe/Admin-Rollen), PW manuell setzen oder per 🔑 PW-Mail, 2FA zurücksetzen, Sperren/Entsperren, Löschen
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

Für alle Admin-Rollen ist 2FA mit einer Authenticator-App (z.B. Google Authenticator, Microsoft
Authenticator, 1Password) **Pflicht**. Normale Benutzer brauchen keine 2FA.

- **Einrichtung beim Login:** Meldet sich jemand mit Admin-Rolle ohne aktive 2FA an, leitet die App
  direkt auf die Einrichtungsseite weiter. Dort erscheint ein QR-Code (alternativ der Schlüssel zum
  Abtippen); nach Eingabe eines gültigen Codes ist 2FA aktiv.
- **Wiederherstellungscodes:** Nach der Aktivierung werden einmalig 10 Recovery-Codes angezeigt.
  Sie ersetzen beim Login den Authenticator-Code, falls das Gerät verloren geht – sicher aufbewahren.
- **Durchsetzung:** Die Policy `RequireTwoFactorForAdmins` prüft auf jeder Admin-Seite, ob 2FA aktiv
  ist. Ohne 2FA bleiben die Admin-Seiten gesperrt, auch wenn die Rolle vergeben ist.
- **Zurücksetzen:** Ein `Admin` kann die 2FA eines Benutzers unter `/admin/users` zurücksetzen
  (z.B. bei Gerätewechsel ohne Recovery-Codes). Die Person richtet 2FA beim nächsten Login neu ein.
- **Reihenfolge beim ersten Login:** Ein automatisch generiertes Passwort muss zuerst geändert
  werden, danach folgt die 2FA-Einrichtung.

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
`pfadiflomi.ch`. Am Proxy `www` per 301 auf `https://pfadiflomi.ch` umleiten, dann reicht die eine URL.

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
Löschfrist der Möbelfotos. Sie laufen gegen eine
SQLite-In-Memory-Datenbank, keine echte MySQL-Instanz nötig:

```bash
dotnet test FlomiApp.sln
```

Das Deploy-Workflow (`.github/workflows/deploy.yaml`) führt `dotnet test` vor dem Publish aus —
ein fehlschlagender Test stoppt den Deploy.

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
