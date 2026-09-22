using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Application.Services;

public interface IMeshMessageProcessor
{
    Task ProcessMeshMessagesAsync(CancellationToken cancellationToken);
    Task AcknowledgeMessageAsync(string messageId, CancellationToken cancellationToken);
}

public class MeshMessageProcessor(
    ILogger<MeshMessageProcessor> logger,
    IMeshInboxClient meshInboxClient
) : IMeshMessageProcessor
{
    /// <summary>
    /// A single execution drains whatever is waiting in the MESH mailbox and then completes.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task ProcessMeshMessagesAsync(CancellationToken cancellationToken)
    {
        var messageIds = await meshInboxClient.GetMessageIdsAsync(cancellationToken);

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

    public Task AcknowledgeMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        return meshInboxClient.AcknowledgeMessageAsync(messageId, cancellationToken);
    }

    private async Task ProcessMessageAsync(string messageId, CancellationToken cancellationToken)
    {
        var message = await meshInboxClient.ReadMessageAsync(messageId, cancellationToken);

        if (!MeshNotificationParser.TryParse(message.Content, out var notification))
        {
            // Leaving the message unacknowledged keeps it in the mailbox so MESH redelivers it
            // rather than the change event being silently dropped. The cost is that it is re-read
            // and re-logged on every run, so a repeat of this entry needs a human.
            logger.LogError(
                "MESH message {MessageId} could not be parsed and was left unacknowledged",
                messageId
            );
            return;
        }

        // The body carries an NHS number in its additional-context.subject part, so it is never
        // logged; the MESH and Bundle identifiers are enough to tie this entry to the notification.
        logger.LogInformation(
            "MESH message {MessageId} received carrying pds-record-change-2 Bundle {BundleId}",
            message.MessageId,
            notification.Id
        );

        // Later workstreams will read the NHS number out of the Bundle and broadcast the change to
        // suppliers through the Webhooks boundary here. Acknowledgement must stay after that step:
        // acknowledging removes the message from the MESH mailbox, so acknowledging before a
        // successful broadcast would lose it.

        // logger.LogInformation("MESH message {MessageId} acknowledged", message.MessageId);
    }
}
