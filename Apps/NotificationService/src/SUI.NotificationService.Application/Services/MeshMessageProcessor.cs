using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Application.Services;

public interface IMeshMessageProcessor
{
    Task ProcessMeshMessagesAsync(CancellationToken cancellationToken);
}

public class MeshMessageProcessor(
    ILogger<MeshMessageProcessor> logger,
    IMeshMessageReceiver messageReceiver
) : IMeshMessageProcessor
{
    /// <summary>
    /// A single execution drains whatever is waiting in the MESH mailbox and then completes.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task ProcessMeshMessagesAsync(CancellationToken cancellationToken)
    {
        var messageIds = await messageReceiver.GetMessageIdsAsync(cancellationToken);

        logger.LogInformation(
            "MESH mailbox holds {MessageCount} message(s) to read in this execution",
            messageIds.Count
        );

        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await ProcessMessageAsync(messageId, cancellationToken);
        }
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

        // await messageReceiver.AcknowledgeMessageAsync(message.MessageId, cancellationToken);

        logger.LogInformation("MESH message {MessageId} acknowledged", message.MessageId);
    }
}
