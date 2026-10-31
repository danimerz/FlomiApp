# CLAUDE.md

FlomiApp: Verwaltungs-App für den Flomi-Markt (Flohmarkt der Pfadi-Abteilung). .NET 10 / ASP.NET Core 10 **Blazor Server**, EF Core 9 + **MySQL 8** (Pomelo 9 – EF-Core-Pakete bewusst auf 9.0.x, bis Pomelo EF Core 10 unterstützt), ASP.NET Identity. Funktionsübersicht: `README.md`.

## Befehle

```bash
dotnet build FlomiApp.sln
dotnet test FlomiApp.sln                 # xUnit, SQLite in-memory – kein MySQL nötig
dotnet run                               # https://localhost:5001
dotnet ef migrations add <Name>          # Migrationen werden beim App-Start automatisch angewendet (MigrateAsync)
```

Connection-String und `AdminPassword` kommen aus User Secrets – nicht in `appsettings.json` eintragen.

## Struktur

- `Pages/Admin/*.razor`, `Pages/User/*.razor` – Blazor-Seiten, je mit eigenem `.razor.css` (scoped CSS)
- `Areas/Identity/Pages/Account/` – angepasste Identity-Seiten (Razor Pages, inkl. 2FA-Setup, Pflicht-PW-Änderung)
- `Services/` – Geschäftslogik, immer Interface + Implementierung (`IXService`/`XService`), Registrierung in `Program.cs`
- `Data/Models/` – EF-Entitäten, `Data/ApplicationDbContext.cs`
- `Security/Roles.cs` – Rollen-Konstanten; `Security/AuditActions.cs` – Audit-Log-Texte
- `Security/ExternalAuth.cs` – Google/Microsoft/MiData-Login, nur aktiv wenn unter `ExternalAuth` konfiguriert; Ablauf in `Areas/Identity/Pages/Account/ExternalLogin` und `LinkLogin`. Admin-Rollen sind von externen Logins ausgeschlossen.
- Passkeys: WebAuthn-Prüfung von Identity 10 (`SignInManager.*Passkey*`), Speicherung im eigenen `Security/ApplicationUserStore` (Tabelle `UserPasskeys`, weil der EF-Store dafür EF Core 10 bräuchte). Regeln/Konstanten in `Security/Passkeys.cs`, Endpunkte `Endpoints/PasskeyEndpoints.cs`, Browser-Teil `wwwroot/js/passkey.js`, Anmeldung im Handler `Passkey` der Login-Seite, Verwaltung `IPasskeyService`. Passkey-Sitzungen tragen `amr=passkey` und erfüllen die Admin-2FA-Policy.
- `Helpers/` – reine Hilfsfunktionen (Telefon, Zeit, Bild-Signatur, Foto-Aufbereitung mit SkiaSharp)
- Hintergrund-Jobs: `MailQueueService`, `PickupImageMaintenanceService`, `ReminderBackgroundService` (Erinnerung am Vortag um 18:00, Logik in `ReminderService`, Zeitpunkt in `Helpers/ReminderSchedule`)
- `Endpoints/` – Minimal-API-Endpunkte (Möbelfotos unter `/pickup-images/{id}`, News-Bilder unter `/news-images/{id}`, Passkeys unter `/passkeys/*`, robots/sitemap)
- Routenplanung: `Helpers/RouteOptimizer.cs` (reine Tourenoptimierung, getestet), `Services/RoutePlanningService.cs` (Adresssuche `GeocodingService` via geo.admin.ch, Fahrzeiten `TravelTimeService` via OpenRouteService oder Luftlinie). Beispieldaten: `SampleDataService` (`Event.IsSample`/`Vehicle.IsSample`)
- `FlomiApp.Tests/` – Testprojekt; wird in `FlomiApp.csproj` explizit vom Glob ausgeschlossen

## Konventionen

- UI-Texte, Kommentare und Audit-Texte auf **Deutsch** (Schweizer Schreibweise: «ss» statt «ß», «Guillemets»).
- Knöpfe und Links nur mit Emoji/Symbol (✕, 🗑️, ‹ ›, ▶) brauchen ein deutsches `aria-label` für Screenreader.
- Admin-Seiten: `[Authorize(Roles = Roles.AdminOr…)]` **plus** `[Authorize(Policy = Roles.RequireTwoFactorForAdmins)]`. Rollen: `Admin`, `FahrzeugAdmin`, `MoebelAdmin`, `BereichsAdmin`.
- Admin-Aktionen mit Tragweite (löschen, sperren, Rollen, Stornierung) über `IAuditLogService` protokollieren.
- Mails nie synchron senden – über `IMailQueue` (Hintergrund-Queue `MailQueueService`). Mails an Admins (neues Konto, Anmeldung/Stornierung) über `IAdminNotificationService`: Empfänger sind Rolle `Admin` mit `NotifyNewUsers`/`NotifyRegistrations`; Benutzereingaben in Mail-HTML immer kodieren.
- Möbelfotos nie roh speichern – immer über `PickupImageProcessor` (verkleinert, entfernt GPS). Bilddaten liegen per Table Splitting in `FurniturePickupImageData`; Listen laden sie nicht mit, angezeigt wird über `/pickup-images/{id}?thumb=true` statt Base64. Gleiches gilt für News-Bilder (`NewsImage`/`NewsImageData`).
- Farben nie fest eintragen: `--brand-*`-Tokens (`site.css`, je Theme in `wwwroot/css/themes/` überschrieben) bzw. `--color-*`. Theme-Auswahl über `Design:Theme` (`Helpers/DesignTheme.cs`). Ausnahme: Mail-HTML braucht Hex-Werte.
- CSP in `Program.cs`: externe Scripts nur von `cdn.jsdelivr.net` (Chart.js, jsQR). Neue externe Quellen dort ergänzen.
- `ChatService` ist Singleton (Echtzeit-Events), die übrigen Services sind Scoped.
- Buchungslogik (`AppointmentService`, `VehicleService`) ist durch Tests abgedeckt – bei Änderungen Tests anpassen/ergänzen.

## Deploy

GitHub Actions:
- `deploy_famMerz.yaml` – **wird aktuell verwendet.** Läuft bei jedem Push auf `master` (ausser nur `.md`) und manuell. Setzt `Version=1.0.<run_number>` (Footer, `Helpers/AppVersion.cs`). Tests, framework-abhängiger Publish als ZIP an `apps.familie-merz.ch` (Server braucht die passende ASP.NET Core Runtime)
- `deploy.yaml` – nur manuell. Tests, self-contained Publish, rsync per SSH, `systemctl restart flomi` (Environment «Brahmsnet»)
