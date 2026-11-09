using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(
    IMeshMessageProcessor meshMessageProcessor,
    ISupplierWebhookRepository supplierRepository,
    IWebhookBroadcaster webhookBroadcaster,
    ILogger<NotificationOrchestrator> logger
) : INotificationOrchestrator
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Notification Service execution started");

        var notifications = await meshMessageProcessor.ProcessMeshMessagesAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            logger.LogInformation("No notifications required processing. Execution completed.");
            return;
        }

        var enabledSuppliers = (
            await supplierRepository.GetAllEnabledAsync(cancellationToken)
        ).ToList();

        if (enabledSuppliers.Count == 0)
        {
            logger.LogWarning(
                "There are no enabled suppliers in the register. No deliveries will be attempted."
            );
            return;
        }

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Processing delivery broadcast for Event {EventId} (MESH Message {MessageId})",
                    notification.EventId,
                    notification.MessageId
                );
            }

            var requiresRetry = await webhookBroadcaster.BroadcastAsync(
                notification,
                enabledSuppliers,
                cancellationToken
            );

            if (!requiresRetry)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Event {EventId} requires no further retries. Acknowledging MESH message {MessageId}.",
                        notification.EventId,
                        notification.MessageId
                    );
                }

                await meshMessageProcessor.AcknowledgeMessageAsync(
                    notification.MessageId,
                    cancellationToken
                );
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Event {EventId} requires retry and will remain unacknowledged in the MESH mailbox.",
                        notification.EventId
                    );
                }
            }
        }

        logger.LogInformation("Notification Service execution completed.");
    }
}
