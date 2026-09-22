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

    /// <summary>
    /// Acknowledges a message in the MESH mailbox so that it is removed from the inbox.
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
            // and re-logged on every run, so a repeat of this entry needs a manual check.
            logger.LogError(
                "MESH message {MessageId} could not be parsed and was left unacknowledged",
                messageId
            );
            return;
        }

        logger.LogInformation(
            "MESH message {MessageId} received carrying pds-record-change-2 Bundle {BundleId}",
            message.MessageId,
            notification.Id
        );
    }
}
