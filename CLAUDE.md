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
- `Helpers/` – reine Hilfsfunktionen (Telefon, Zeit, Bild-Signatur, Foto-Aufbereitung mit SkiaSharp)
- `Endpoints/` – Minimal-API-Endpunkte (Möbelfotos unter `/pickup-images/{id}`)
- `FlomiApp.Tests/` – Testprojekt; wird in `FlomiApp.csproj` explizit vom Glob ausgeschlossen

## Konventionen

- UI-Texte, Kommentare und Audit-Texte auf **Deutsch** (Schweizer Schreibweise: «ss» statt «ß», «Guillemets»).
- Admin-Seiten: `[Authorize(Roles = Roles.AdminOr…)]` **plus** `[Authorize(Policy = Roles.RequireTwoFactorForAdmins)]`. Rollen: `Admin`, `FahrzeugAdmin`, `MoebelAdmin`, `BereichsAdmin`.
- Admin-Aktionen mit Tragweite (löschen, sperren, Rollen, Stornierung) über `IAuditLogService` protokollieren.
- Mails nie synchron senden – über `IMailQueue` (Hintergrund-Queue `MailQueueService`).
- Möbelfotos nie roh speichern – immer über `PickupImageProcessor` (verkleinert, entfernt GPS). Bilddaten liegen per Table Splitting in `FurniturePickupImageData`; Listen laden sie nicht mit, angezeigt wird über `/pickup-images/{id}?thumb=true` statt Base64.
- CSP in `Program.cs`: externe Scripts nur von `cdn.jsdelivr.net` (Chart.js, jsQR). Neue externe Quellen dort ergänzen.
- `ChatService` ist Singleton (Echtzeit-Events), die übrigen Services sind Scoped.
- Buchungslogik (`AppointmentService`, `VehicleService`) ist durch Tests abgedeckt – bei Änderungen Tests anpassen/ergänzen.

## Deploy

GitHub Actions, nur manuell (`workflow_dispatch`):
- `deploy_famMerz.yaml` – **wird aktuell verwendet.** Tests, framework-abhängiger Publish als ZIP an `apps.familie-merz.ch` (Server braucht die passende ASP.NET Core Runtime)
- `deploy.yaml` – Tests, self-contained Publish, rsync per SSH, `systemctl restart flomi` (Environment «Brahmsnet»)
