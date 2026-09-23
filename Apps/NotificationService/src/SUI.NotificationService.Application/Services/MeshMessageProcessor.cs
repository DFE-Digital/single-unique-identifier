using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Services;

public interface IMeshMessageProcessor
{
    Task<IReadOnlyList<PdsRecordChangeNotification>> ProcessMeshMessagesAsync(
        CancellationToken cancellationToken
    );
    Task AcknowledgeMessageAsync(string messageId, CancellationToken cancellationToken);
}

public class MeshMessageProcessor(
    ILogger<MeshMessageProcessor> logger,
    IMeshInboxClient meshInboxClient
) : IMeshMessageProcessor
{
    /// <summary>
    /// A single execution drains whatever is waiting in the MESH mailbox and then completes,
    /// returning the record changes it understood so the caller can act on them. Messages that
    /// could not be read or understood are left out and stay in the mailbox.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task<IReadOnlyList<PdsRecordChangeNotification>> ProcessMeshMessagesAsync(
        CancellationToken cancellationToken
    )
    {
        var messageIds = await meshInboxClient.GetMessageIdsAsync(cancellationToken);

        logger.LogInformation(
            "MESH mailbox holds {MessageCount} message(s) to read in this execution",
            messageIds.Count
        );

        var notifications = new List<PdsRecordChangeNotification>();

        foreach (var messageId in messageIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var notification = await ProcessMessageAsync(messageId, cancellationToken);

                if (notification is not null)
                {
                    notifications.Add(notification);
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                // One unreadable message must not cost this execution the rest of the mailbox. The
                // message stays unacknowledged, so MESH redelivers it on the next run. This also
                // catches TaskCanceledException raised by HttpClient's own request timeout, which
                // is an OperationCanceledException but unrelated to this execution's cancellation
                // token; only cancellation of that token is left to end the execution early.
                logger.LogError(
                    exception,
                    "MESH message {MessageId} could not be processed and was left unacknowledged",
                    messageId
                );
            }
        }

        return notifications;
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

    private async Task<PdsRecordChangeNotification?> ProcessMessageAsync(
        string messageId,
        CancellationToken cancellationToken
    )
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
            return null;
        }

        if (!MeshNotificationParser.TryGetNhsNumber(notification, out var nhsNumber))
        {
            // Without an NHS number there is nothing to distribute, so the message is left
            // unacknowledged for the same reason as an unparseable one.
            logger.LogError(
                "MESH message {MessageId} carried no NHS number and was left unacknowledged",
                messageId
            );
            return null;
        }

        logger.LogInformation(
            "MESH message {MessageId} received carrying pds-record-change-2 Bundle {BundleId}",
            message.MessageId,
            notification.Id
        );

        return new PdsRecordChangeNotification(message.MessageId, nhsNumber);
    }
}
