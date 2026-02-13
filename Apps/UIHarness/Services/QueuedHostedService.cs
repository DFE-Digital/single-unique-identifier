using UIHarness.Interfaces;
using Microsoft.Extensions.Options;

namespace UIHarness.Services;

public sealed class QueuedHostedService(
    IBackgroundTaskQueue queue,
    IOptions<BackgroundWorkerOptions> options,
    ILogger<QueuedHostedService> logger) : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    private readonly BackgroundWorkerOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<QueuedHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = new List<Task>();

        var workerCount = _options.WorkerCount <= 0 ? 1 : _options.WorkerCount;

        for (var i = 0; i < workerCount; i++)
        {
            workers.Add(Task.Run(() => RunWorkerAsync(stoppingToken), stoppingToken));
        }

        await Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, Task> workItem;

            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background work item failed.");
            }
        }
    }
}
