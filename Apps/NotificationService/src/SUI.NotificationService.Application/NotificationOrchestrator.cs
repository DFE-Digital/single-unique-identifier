using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(
    IMeshMessageProcessor meshMessageProcessor,
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

        var aggregateResults = await webhookBroadcaster.BroadcastBatchAsync(
            notifications,
            cancellationToken
        );

        await AcknowledgeSuccessfulMessagesAsync(aggregateResults, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Notification Service execution completed with {NotificationCount} record change notification(s).",
                notifications.Count
            );
        }
    }

    private async Task AcknowledgeSuccessfulMessagesAsync(
        IEnumerable<BroadcastAggregateResult> aggregateResults,
        CancellationToken cancellationToken
    )
    {
        foreach (var result in aggregateResults)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (result.EndpointsRequiringNewDeliveryAttempt == 0)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Event {EventId} requires no further retries. MESH message {MessageId} can be acknowledged.",
                        result.EventId,
                        result.MessageId
                    );
                }

                await meshMessageProcessor.AcknowledgeMessageAsync(
                    result.MessageId,
                    cancellationToken
                );
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Event {EventId} has {StillRequiring} endpoint(s) still requiring successful delivery. MESH message {MessageId} will remain unacknowledged in the MESH mailbox.",
                        result.EventId,
                        result.EndpointsRequiringNewDeliveryAttempt,
                        result.MessageId
                    );
                }
            }
        }
    }
}
