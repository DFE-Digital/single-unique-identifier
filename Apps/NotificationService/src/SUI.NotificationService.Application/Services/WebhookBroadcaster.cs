using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Services;

internal sealed class WebhookBroadcaster(
    ISupplierWebhookDeliveryService deliveryService,
    IWebhookReceiptRepository receiptRepository,
    TimeProvider timeProvider,
    ILogger<WebhookBroadcaster> logger
) : IWebhookBroadcaster
{
    public async Task<IReadOnlyList<string>> BroadcastBatchAsync(
        IReadOnlyList<PdsRecordChangeNotification> notifications,
        IReadOnlyList<SupplierWebhook> suppliers,
        CancellationToken cancellationToken
    )
    {
        var ackableMessageIds = new List<string>();

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceEventId = notification.EventId.ToString();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Processing delivery broadcast for Event {EventId} (MESH Message {MessageId})",
                    notification.EventId,
                    notification.MessageId
                );
            }

            // Track the outcomes of all attempted (and skipped) deliveries for the aggregate summary
            var broadcastOutcomes = new List<DeliveryOutcome>();

            foreach (var supplier in suppliers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check Receipt Store
                if (
                    await receiptRepository.ExistsAsync(
                        sourceEventId,
                        supplier.SupplierId,
                        cancellationToken
                    )
                )
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Skipping Supplier {SupplierId}: Prior successful delivery receipt found.",
                            supplier.SupplierId
                        );
                    }

                    // Record the Skip
                    broadcastOutcomes.Add(DeliveryOutcome.SkippedPreviouslyAcknowledged);
                    continue;
                }

                // Prepare Delivery
                var stableDeliveryId = GenerateDeterministicGuid(
                    sourceEventId,
                    supplier.SupplierId
                );
                var request = new WebhookDeliveryRequest(
                    DeliveryId: stableDeliveryId,
                    SourceEventId: notification.EventId,
                    EventType: notification.EventType,
                    OccurredAt: notification.OccurredAt,
                    AffectedNhsNumber: notification.NhsNumber,
                    EndpointUrl: supplier.EndpointUrl,
                    SecretKeyVaultReference: supplier.SecretKeyVaultReference,
                    CorrelationId: Guid.NewGuid()
                );

                // Attempt Delivery
                var result = await deliveryService.DeliverAsync(request, cancellationToken);
                broadcastOutcomes.Add(result.Outcome);

                // Classify and Act
                switch (result.Outcome)
                {
                    case DeliveryOutcome.Success:
                        var receipt = new WebhookDeliveryReceipt(
                            SourceEventId: sourceEventId,
                            SupplierId: supplier.SupplierId,
                            DeliveryId: stableDeliveryId,
                            ContractVersion: supplier.ContractVersion,
                            AcknowledgedAt: timeProvider.GetUtcNow(),
                            HttpStatusCode: result.HttpStatusCode!.Value
                        );
                        await receiptRepository.AddAsync(receipt, cancellationToken);
                        break;

                    case DeliveryOutcome.RetryableFailure:
                        logger.LogWarning(
                            "Delivery to Supplier {SupplierId} failed with a retryable error.",
                            supplier.SupplierId
                        );
                        break;

                    case DeliveryOutcome.PermanentFailure:
                        logger.LogError(
                            "Delivery to Supplier {SupplierId} failed with a permanent error ({StatusCode}).",
                            supplier.SupplierId,
                            result.HttpStatusCode
                        );
                        break;

                    case DeliveryOutcome.SkippedPreviouslyAcknowledged:
                        break;

                    default:
                        logger.LogWarning(
                            "Unknown delivery outcome {Outcome} for Supplier {SupplierId}",
                            result.Outcome,
                            supplier.SupplierId
                        );
                        break;
                }
            }

            var newlySuccessful = broadcastOutcomes.Count(o => o == DeliveryOutcome.Success);
            var skipped = broadcastOutcomes.Count(o =>
                o == DeliveryOutcome.SkippedPreviouslyAcknowledged
            );
            var retryable = broadcastOutcomes.Count(o => o == DeliveryOutcome.RetryableFailure);
            var permanent = broadcastOutcomes.Count(o => o == DeliveryOutcome.PermanentFailure);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Broadcast aggregate for Event {EventId}: {NewlySuccessful} newly successful, {Skipped} skipped (previously acknowledged), {Retryable} retryable failures, {Permanent} permanent failures.",
                    sourceEventId,
                    newlySuccessful,
                    skipped,
                    retryable,
                    permanent
                );
            }

            if (retryable == 0)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Event {EventId} requires no further retries. MESH message {MessageId} can be acknowledged.",
                        notification.EventId,
                        notification.MessageId
                    );
                }

                ackableMessageIds.Add(notification.MessageId);
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Event {EventId} requires retry ({Retryable} endpoints failing) and will remain unacknowledged in the MESH mailbox.",
                        notification.EventId,
                        retryable
                    );
                }
            }
        }

        return ackableMessageIds;
    }

    private static Guid GenerateDeterministicGuid(string eventId, string supplierId)
    {
        var input = Encoding.UTF8.GetBytes($"{eventId}:{supplierId}");
        var hash = SHA256.HashData(input);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
