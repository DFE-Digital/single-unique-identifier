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
    public async Task<IReadOnlyList<BroadcastAggregateResult>> BroadcastBatchAsync(
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

        var aggregateResults = new List<BroadcastAggregateResult>();

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ProcessSingleNotificationAsync(
                notification,
                enabledSuppliers,
                cancellationToken
            );

            aggregateResults.Add(result);
        }

        return aggregateResults;
    }

    private async Task<BroadcastAggregateResult> ProcessSingleNotificationAsync(
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

        var endpointResults = new List<EndpointDeliveryResult>();

        foreach (var supplier in suppliers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outcome = await DeliverToSupplierAsync(
                notification,
                supplier,
                sourceEventId,
                cancellationToken
            );

            endpointResults.Add(new EndpointDeliveryResult(supplier.SupplierId, outcome));
        }

        return BuildAggregateResult(notification, suppliers.Count, endpointResults);
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
        WebhookDeliveryResult result;
        try
        {
            result = await deliveryService.DeliverAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "An unexpected exception occurred while attempting delivery to Supplier {SupplierId}. This delivery will be marked as a permanent failure.",
                supplier.SupplierId
            );

            // Treat unhandled supplier-specific code/config exceptions as permanent failures
            // so they block MESH acknowledgement but allow other suppliers to proceed.
            return DeliveryOutcome.PermanentFailure;
        }

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

    private BroadcastAggregateResult BuildAggregateResult(
        PdsRecordChangeNotification notification,
        int totalSuppliers,
        List<EndpointDeliveryResult> endpointResults
    )
    {
        var newlySuccessful = endpointResults.Count(e => e.Outcome == DeliveryOutcome.Success);
        var skipped = endpointResults.Count(e =>
            e.Outcome == DeliveryOutcome.SkippedPreviouslyAcknowledged
        );
        var retryable = endpointResults.Count(e => e.Outcome == DeliveryOutcome.RetryableFailure);
        var permanent = endpointResults.Count(e => e.Outcome == DeliveryOutcome.PermanentFailure);

        var failed = retryable + permanent;
        var endpointsStillRequiringSuccessfulDelivery = failed;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Broadcast aggregate for Event {EventId}: {NewlySuccessful} newly successful, {Skipped} skipped, {Retryable} retryable failures, {Permanent} permanent failures.",
                notification.EventId,
                newlySuccessful,
                skipped,
                retryable,
                permanent
            );
        }

        return new BroadcastAggregateResult
        {
            EventId = notification.EventId,
            MessageId = notification.MessageId,
            TotalEnabledSuppliers = totalSuppliers,
            NewlySuccessful = newlySuccessful,
            FailedDeliveries = failed,
            SkippedPreviouslyAcknowledged = skipped,
            EndpointsRequiringNewDeliveryAttempt = endpointsStillRequiringSuccessfulDelivery,
            EndpointResults = endpointResults,
        };
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
