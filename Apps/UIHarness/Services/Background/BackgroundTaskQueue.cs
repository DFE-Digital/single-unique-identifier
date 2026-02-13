using System.Threading.Channels;
using UIHarness.Interfaces;

namespace UIHarness.Services;

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    public ValueTask QueueAsync(Func<CancellationToken, Task> workItem, CancellationToken cancellationToken)
    {
        if (workItem is null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        return _queue.Writer.WriteAsync(workItem, cancellationToken);
    }

    public ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
