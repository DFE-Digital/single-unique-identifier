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

        var ackableMessageIds = await webhookBroadcaster.BroadcastBatchAsync(
            notifications,
            enabledSuppliers,
            cancellationToken
        );

        // Acknowledge the messages that fully succeeded
        foreach (var messageId in ackableMessageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await meshMessageProcessor.AcknowledgeMessageAsync(messageId, cancellationToken);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Notification Service execution completed with {NotificationCount} record change notification(s).",
                notifications.Count
            );
        }
    }
}
