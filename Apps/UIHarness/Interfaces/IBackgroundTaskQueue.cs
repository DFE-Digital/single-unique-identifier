namespace UIHarness.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken);
    ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}