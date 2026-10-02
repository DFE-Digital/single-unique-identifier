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
            catch (Exception exception)
                when (IsExpectedTransportFailure(exception, cancellationToken))
            {
                // One unreadable message must not cost this execution the rest of the mailbox. The
                // message stays unacknowledged, so MESH redelivers it on the next run. Only
                // per-message transport failures are tolerated here; programming faults and fatal
                // exceptions propagate to NotificationServiceRunner so the run fails visibly.
                logger.LogError(
                    exception,
                    "MESH message {MessageId} could not be processed and was left unacknowledged",
                    messageId
                );
            }
        }

        (notifications, var duplicateMessageIds) = SeparateDuplicates(notifications);

        return notifications;
    }

    // HttpClient surfaces a non-success status as HttpRequestException and its own request timeout
    // as TaskCanceledException, which is an OperationCanceledException unrelated to this
    // execution's token; cancellation of that token is left to end the execution early.
    private static bool IsExpectedTransportFailure(
        Exception exception,
        CancellationToken cancellationToken
    ) =>
        exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => !cancellationToken.IsCancellationRequested,
            _ => false,
        };

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

    /// <summary>
    /// Returns 2 lists. 1 of non duplicate notifications and 1 of duplicate message ids.
    /// </summary>
    /// <returns></returns>
    private static (
        List<PdsRecordChangeNotification> notifications,
        List<string> duplicateMessageIds
    ) SeparateDuplicates(List<PdsRecordChangeNotification> notifications)
    {
        var nonDuplicateNotifications = new List<PdsRecordChangeNotification>();
        var duplicateMessageIds = new List<string>();

        var seenNhsNumbers = new HashSet<string>();

        foreach (var notification in notifications)
        {
            if (!seenNhsNumbers.Add(notification.NhsNumber))
            {
                duplicateMessageIds.Add(notification.MessageId);
            }
            else
            {
                nonDuplicateNotifications.Add(notification);
            }
        }

        return (nonDuplicateNotifications, duplicateMessageIds);
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
            // Without a valid NHS number there is nothing to distribute, so the message is left
            // unacknowledged for the same reason as an unparseable one.
            logger.LogError(
                "MESH message {MessageId} carried no valid NHS number and was left unacknowledged",
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
