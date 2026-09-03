using Microsoft.Extensions.Logging;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(ILogger<NotificationOrchestrator> logger)
    : INotificationOrchestrator
{
    public Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Notification Service execution started");

        // Later workstreams will extend this orchestration point to receive lifecycle changes
        // through the MNS boundary, coordinate them in Application, and broadcast supplier
        // notifications through the Webhooks boundary before this finite execution completes.

        logger.LogInformation("Notification Service execution completed");

        return Task.CompletedTask;
    }
}
