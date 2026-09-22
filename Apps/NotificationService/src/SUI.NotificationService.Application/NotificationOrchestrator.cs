using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(
    IMeshMessageProcessor meshMessageProcessor,
    ILogger<NotificationOrchestrator> logger
) : INotificationOrchestrator
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Notification Service execution started");

        var notifications = await meshMessageProcessor.ProcessMeshMessagesAsync(cancellationToken);

        logger.LogInformation(
            "Notification Service execution completed with {NotificationCount} record change notification(s)",
            notifications.Count
        );
    }
}
