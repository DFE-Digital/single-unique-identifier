using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application;

internal sealed class NotificationOrchestrator(
    IMeshMessageProcessor meshMessageProcessor,
    ISupplierWebhookRepository supplierRepository,
    ISupplierWebhookDeliveryService deliveryService,
    IWebhookReceiptRepository receiptRepository,
    TimeProvider timeProvider,
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

        foreach (var notification in notifications)
        {
            var sourceEventId = notification.EventId.ToString();

            logger.LogInformation(
                "Processing delivery broadcast for Event {EventId} (MESH Message {MessageId})",
                sourceEventId,
                notification.MessageId
            );

            // Track the outcomes of all attempted (and skipped) deliveries for the aggregate summary
            var broadcastOutcomes = new List<DeliveryOutcome>();

            foreach (var supplier in enabledSuppliers)
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
                    logger.LogInformation(
                        "Skipping Supplier {SupplierId}: Prior successful delivery receipt found.",
                        supplier.SupplierId
                    );

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
                    DeliveryAttempt: 1,
                    KeyId: "key1",
                    CorrelationId: Guid.NewGuid()
                );

                // Attempt Delivery
                var result = await deliveryService.DeliverAsync(request, cancellationToken);

                // Record the Outcome
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
                }
            }

            // Final MESH Evaluation & Aggregation
            var newlySuccessful = broadcastOutcomes.Count(o => o == DeliveryOutcome.Success);
            var skipped = broadcastOutcomes.Count(o =>
                o == DeliveryOutcome.SkippedPreviouslyAcknowledged
            );
            var retryable = broadcastOutcomes.Count(o => o == DeliveryOutcome.RetryableFailure);
            var permanent = broadcastOutcomes.Count(o => o == DeliveryOutcome.PermanentFailure);

            // Log the aggregate result
            logger.LogInformation(
                "Broadcast aggregate for Event {EventId}: {NewlySuccessful} newly successful, {Skipped} skipped (previously acknowledged), {Retryable} retryable failures, {Permanent} permanent failures.",
                sourceEventId,
                newlySuccessful,
                skipped,
                retryable,
                permanent
            );

            if (retryable == 0)
            {
                logger.LogInformation(
                    "Event {EventId} requires no further retries. Acknowledging MESH message {MessageId}.",
                    sourceEventId,
                    notification.MessageId
                );
                await meshMessageProcessor.AcknowledgeMessageAsync(
                    notification.MessageId,
                    cancellationToken
                );
            }
            else
            {
                logger.LogInformation(
                    "Event {EventId} requires retry ({Retryable} endpoints failing) and will remain unacknowledged in the MESH mailbox.",
                    sourceEventId,
                    retryable
                );
            }
        }

        logger.LogInformation("Notification Service execution completed.");
    }

    private static Guid GenerateDeterministicGuid(string eventId, string supplierId)
    {
        var input = Encoding.UTF8.GetBytes($"{eventId}:{supplierId}");
        var hash = SHA1.HashData(input);

        // Truncate SHA1 to 16 bytes to form a UUID
        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);

        // Set UUID version to 5 (Name-based SHA1)
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }
}
