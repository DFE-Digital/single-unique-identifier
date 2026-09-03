using Microsoft.Extensions.Logging;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(ILogger<NotificationOrchestrator> logger)
    : INotificationOrchestrator
{
    public Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Notification Service execution started");
        logger.LogInformation("Notification Service execution completed");

        return Task.CompletedTask;
    }
}
