namespace FlomiApp.Services;

/// <summary>
/// Schaut alle 15 Minuten, ob Erinnerungen fällig sind (ab 18:00 für den nächsten Tag). Bereits
/// verschickte sind in <c>Appointment.ReminderSentAt</c> markiert, ein Neustart verschickt also nichts doppelt.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval   = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger)
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
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IReminderService>()
                        .SendDueRemindersAsync(DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erinnerungen am Vortag: Fehler beim Versand");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // App wird beendet
        }
    }
}
