using FlomiApp.Services;

namespace FlomiApp.Tests;

/// <summary>Fake IMailQueue für Tests — zählt nur mit, verschickt nichts.</summary>
public class NoOpMailQueue : IMailQueue
{
    public int EnqueueCount { get; private set; }

    public void Enqueue(Func<IMailService, Task> job) => EnqueueCount++;
}
