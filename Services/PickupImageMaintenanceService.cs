// Services/PickupImageMaintenanceService.cs
namespace FlomiApp.Services;

/// <summary>
/// Hintergrunddienst für Möbelfotos: setzt einmal täglich die Löschfrist um
/// (Fotos 3 Monate nach dem Event) und verkleinert Altbilder aus der Zeit vor
/// der Komprimierung – in kleinen Häppchen, damit der Server nicht ausgebremst wird.
/// </summary>
public class PickupImageMaintenanceService : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval   = TimeSpan.FromHours(24);
    private const int ReprocessBatchSize = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PickupImageMaintenanceService> _logger;

    public PickupImageMaintenanceService(IServiceScopeFactory scopeFactory,
        ILogger<PickupImageMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // App wird beendet
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Zuerst löschen – abgelaufene Fotos müssen nicht mehr verkleinert werden
            using (var scope = _scopeFactory.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IFurniturePickupService>();
                await service.DeleteExpiredImagesAsync(DateTime.Now);
            }

            var total = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                // Pro Häppchen ein frischer Scope, damit der DbContext nicht anwächst
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFurniturePickupService>();
                var count = await service.ReprocessLegacyImagesAsync(ReprocessBatchSize);
                total += count;
                if (count < ReprocessBatchSize) break;
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }

            if (total > 0)
                _logger.LogInformation("{Count} ältere Möbelfotos verkleinert.", total);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Wartung der Möbelfotos fehlgeschlagen.");
        }
    }
}
