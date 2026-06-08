using System.Threading.Channels;

namespace FlomiApp.Services;

public interface IMailQueue
{
    void Enqueue(Func<IMailService, Task> job);
}

public class MailQueueService : BackgroundService, IMailQueue
{
    private readonly Channel<Func<IMailService, Task>> _channel =
        Channel.CreateUnbounded<Func<IMailService, Task>>(
            new UnboundedChannelOptions { SingleReader = true });

    private readonly IServiceScopeFactory            _scopeFactory;
    private readonly ILogger<MailQueueService>        _logger;

    public MailQueueService(IServiceScopeFactory scopeFactory,
        ILogger<MailQueueService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public void Enqueue(Func<IMailService, Task> job) =>
        _channel.Writer.TryWrite(job);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope       = _scopeFactory.CreateScope();
                var       mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
                await job(mailService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail-Queue: Fehler beim Verarbeiten einer Mail");
            }
        }
    }
}
