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

        var (nonDuplicates, duplicates) = SeparateDuplicates(notifications);

        await AcknowledgeDuplicatesAsync(duplicates, cancellationToken);

        return nonDuplicates;
    }

    // HttpClient surfaces a non-success status as HttpRequestException and its own request timeout
    // as TaskCanceledException, which is an OperationCanceledException unrelated to this
    // execution's token; cancellation of that token is left to end the execution early. The
    // resilience pipeline's timeouts surface as TimeoutException (Polly's TimeoutRejectedException).
    private static bool IsExpectedTransportFailure(
        Exception exception,
        CancellationToken cancellationToken
    ) =>
        exception switch
        {
            HttpRequestException => true,
            TimeoutException => true,
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
    /// Splits notifications by NHS number into the first notification seen for each NHS number
    /// (the nonDuplicates) and every later copy (the duplicates), each paired with the message ID of
    /// the remaining it duplicates for logging purposes.
    /// </summary>
    private static (
        List<PdsRecordChangeNotification> NonDuplicates,
        List<(string DuplicateMessageId, string NonDuplicateMessageId)> Duplicates
    ) SeparateDuplicates(List<PdsRecordChangeNotification> notifications)
    {
        var nonDuplicates = new List<PdsRecordChangeNotification>();
        var duplicates = new List<(string DuplicateMessageId, string NonDuplicateMessageId)>();

        var nonDuplicateMessageIdByNhsNumber = new Dictionary<string, string>();

        foreach (var notification in notifications)
        {
            if (
                nonDuplicateMessageIdByNhsNumber.TryAdd(
                    notification.NhsNumber,
                    notification.MessageId
                )
            )
            {
                nonDuplicates.Add(notification);
            }
            else
            {
                duplicates.Add(
                    (
                        notification.MessageId,
                        nonDuplicateMessageIdByNhsNumber[notification.NhsNumber]
                    )
                );
            }
        }

        return (nonDuplicates, duplicates);
    }

    /// <summary>
    /// Acknowledges each duplicate so MESH removes it from the mailbox. This cannot lose a change,
    /// because the non-duplicate carrying the same NHS number stays in the mailbox until it is handled.
    /// </summary>
    private async Task AcknowledgeDuplicatesAsync(
        List<(string DuplicateMessageId, string NonDuplicateMessageId)> duplicates,
        CancellationToken cancellationToken
    )
    {
        if (duplicates.Count == 0)
        {
            return;
        }

        var acknowledgedCount = 0;
        var failedCount = 0;

        foreach (var (duplicateMessageId, nonDuplicateMessageId) in duplicates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await AcknowledgeMessageAsync(duplicateMessageId, cancellationToken);
                acknowledgedCount++;

                logger.LogInformation(
                    "MESH message {DuplicateMessageId} acknowledged as a duplicate of {NonDuplicateMessageId}",
                    duplicateMessageId,
                    nonDuplicateMessageId
                );
            }
            catch (Exception exception)
                when (IsExpectedTransportFailure(exception, cancellationToken))
            {
                // A duplicate left in the mailbox is collapsed again on the next execution, so this
                // corrects itself and must not cost this execution the remaining duplicates.
                failedCount++;

                logger.LogWarning(
                    exception,
                    "MESH message {DuplicateMessageId} could not be acknowledged as a duplicate and was left in the mailbox",
                    duplicateMessageId
                );
            }
        }

        logger.LogInformation(
            "{AcknowledgedCount} duplicate MESH message(s) acknowledged, {FailedCount} acknowledgement(s) failed",
            acknowledgedCount,
            failedCount
        );
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
