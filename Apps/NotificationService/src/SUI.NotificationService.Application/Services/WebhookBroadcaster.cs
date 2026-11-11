using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Services;

internal sealed class WebhookBroadcaster(
    ISupplierWebhookRepository supplierRepository,
    ISupplierWebhookDeliveryService deliveryService,
    IWebhookReceiptRepository receiptRepository,
    TimeProvider timeProvider,
    ILogger<WebhookBroadcaster> logger
) : IWebhookBroadcaster
{
    public async Task<IReadOnlyList<string>> BroadcastBatchAsync(
        IReadOnlyList<PdsRecordChangeNotification> notifications,
        CancellationToken cancellationToken
    )
    {
        var enabledSuppliers = (
            await supplierRepository.GetAllEnabledAsync(cancellationToken)
        ).ToList();

        if (enabledSuppliers.Count == 0)
        {
            logger.LogWarning(
                "There are no enabled suppliers in the register. No deliveries will be attempted."
            );
            return [];
        }

        var acknowledgeableMessageIds = new List<string>();

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var canAcknowledge = await ProcessSingleNotificationAsync(
                notification,
                enabledSuppliers,
                cancellationToken
            );

            if (canAcknowledge)
            {
                acknowledgeableMessageIds.Add(notification.MessageId);
            }
        }

        return acknowledgeableMessageIds;
    }

    private async Task<bool> ProcessSingleNotificationAsync(
        PdsRecordChangeNotification notification,
        IReadOnlyList<SupplierWebhook> suppliers,
        CancellationToken cancellationToken
    )
    {
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

            var outcome = await DeliverToSupplierAsync(
                notification,
                supplier,
                sourceEventId,
                cancellationToken
            );
            broadcastOutcomes.Add(outcome);
        }

        return EvaluateBroadcastOutcomes(notification, sourceEventId, broadcastOutcomes);
    }

    private async Task<DeliveryOutcome> DeliverToSupplierAsync(
        PdsRecordChangeNotification notification,
        SupplierWebhook supplier,
        string sourceEventId,
        CancellationToken cancellationToken
    )
    {
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
            return DeliveryOutcome.SkippedPreviouslyAcknowledged;
        }

        // Prepare Delivery
        var stableDeliveryId = GenerateDeterministicGuid(sourceEventId, supplier.SupplierId);
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

        // Classify and Act
        await HandleDeliveryOutcomeAsync(
            result,
            supplier,
            sourceEventId,
            stableDeliveryId,
            cancellationToken
        );

        return result.Outcome;
    }

    private async Task HandleDeliveryOutcomeAsync(
        WebhookDeliveryResult result,
        SupplierWebhook supplier,
        string sourceEventId,
        Guid stableDeliveryId,
        CancellationToken cancellationToken
    )
    {
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

    private bool EvaluateBroadcastOutcomes(
        PdsRecordChangeNotification notification,
        string sourceEventId,
        List<DeliveryOutcome> broadcastOutcomes
    )
    {
        var newlySuccessful = broadcastOutcomes.Count(o => o == DeliveryOutcome.Success);
        var skipped = broadcastOutcomes.Count(o =>
            o == DeliveryOutcome.SkippedPreviouslyAcknowledged
        );
        var retryable = broadcastOutcomes.Count(o => o == DeliveryOutcome.RetryableFailure);
        var permanent = broadcastOutcomes.Count(o => o == DeliveryOutcome.PermanentFailure);
        var endpointsStillRequiringSuccessfulDelivery = retryable + permanent;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Broadcast aggregate for Event {EventId}: {NewlySuccessful} newly successful, {Skipped} skipped, {Retryable} retryable failures, {Permanent} permanent failures. Endpoints still requiring successful delivery: {StillRequiring}.",
                sourceEventId,
                newlySuccessful,
                skipped,
                retryable,
                permanent,
                endpointsStillRequiringSuccessfulDelivery
            );
        }

        if (endpointsStillRequiringSuccessfulDelivery == 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Event {EventId} requires no further retries. MESH message {MessageId} can be acknowledged.",
                    notification.EventId,
                    notification.MessageId
                );
            }
            return true;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Event {EventId} has {StillRequiring} endpoint(s) still requiring successful delivery. MESH message {MessageId} will remain unacknowledged in the MESH mailbox.",
                notification.EventId,
                endpointsStillRequiringSuccessfulDelivery,
                notification.MessageId
            );
        }
        return false;
    }

    private static Guid GenerateDeterministicGuid(string eventId, string supplierId)
    {
        var input = Encoding.UTF8.GetBytes($"{eventId}:{supplierId}");
        var hash = SHA256.HashData(input);

        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Apply RFC 4122 Version 8 bitmask (Custom/Experimental hash-based)
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x80);

        // Apply RFC 4122 Variant bitmask (bits 10xx)
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }
}
