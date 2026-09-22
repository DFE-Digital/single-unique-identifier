using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(
    IMeshMessageReceiver messageReceiver,
    ILogger<NotificationOrchestrator> logger
) : INotificationOrchestrator
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Notification Service execution started");

        // A single execution drains whatever is waiting in the MESH mailbox and then completes. The
        // schedule that starts this process owns how often that happens.
        var messageCount = await messageReceiver.GetMessageCountAsync(cancellationToken);
        var messageIds = await messageReceiver.GetMessageIdsAsync(cancellationToken);

        logger.LogInformation(
            "MESH mailbox holds {MessageCount} message(s); {ReadableCount} available to read in this execution",
            messageCount,
            messageIds.Count
        );

        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ProcessMessageAsync(messageId, cancellationToken);
        }

        logger.LogInformation("Notification Service execution completed");
    }

    private async Task ProcessMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        var message = await messageReceiver.ReadMessageAsync(messageId, cancellationToken);

        logger.LogInformation(
            "MESH message {MessageId} received: {Content}",
            message.MessageId,
            message.Content
        );

        // Later workstreams will parse the pds-record-change-2 Bundle and broadcast the change to
        // suppliers through the Webhooks boundary here. Acknowledgement must stay after that step:
        // acknowledging removes the message from the MESH mailbox, so acknowledging before a
        // successful broadcast would lose it.

        await messageReceiver.AcknowledgeMessageAsync(message.MessageId, cancellationToken);

        logger.LogInformation("MESH message {MessageId} acknowledged", message.MessageId);
    }
}
