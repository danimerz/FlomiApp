using FlomiApp.Services;

namespace FlomiApp.Tests;

/// <summary>Fake IMailQueue für Tests — merkt sich die Aufträge, verschickt nichts.
/// Mit <see cref="RunAllAsync"/> lassen sie sich gegen einen <see cref="RecordingMailService"/> ausführen.</summary>
public class NoOpMailQueue : IMailQueue
{
    private readonly List<Func<IMailService, Task>> _jobs = new();

    public int EnqueueCount => _jobs.Count;

    public void Enqueue(Func<IMailService, Task> job) => _jobs.Add(job);

    public async Task<RecordingMailService> RunAllAsync()
    {
        var mail = new RecordingMailService();
        foreach (var job in _jobs) await job(mail);
        return mail;
    }
}
