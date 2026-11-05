using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Infrastructure.Entities;
using SUI.NotificationService.Infrastructure.Utilities;

namespace SUI.NotificationService.Infrastructure.Repositories;

public class WebhookReceiptRepository(
    TableClient tableClient,
    ILogger<WebhookReceiptRepository> logger
) : IWebhookReceiptRepository
{
    public async Task AddAsync(
        WebhookDeliveryReceipt receipt,
        CancellationToken cancellationToken = default
    )
    {
        // Uniqueness constraint: PartitionKey is EventId, RowKey is SupplierId
        var partitionKey = TableKeyNormaliser.Normalise(receipt.SourceEventId);
        var rowKey = TableKeyNormaliser.Normalise(receipt.SupplierId);

        var entity = new WebhookDeliveryReceiptEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            SourceEventId = receipt.SourceEventId,
            SupplierId = receipt.SupplierId,
            DeliveryId = receipt.DeliveryId,
            ContractVersion = receipt.ContractVersion,
            AcknowledgedAt = receipt.AcknowledgedAt,
            HttpStatusCode = receipt.HttpStatusCode,
        };

        try
        {
            await tableClient.AddEntityAsync(entity, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 409)
        {
            // Idempotent creation: If multiple threads try to write the same success receipt, it's fine.
            logger.LogDebug(
                "Receipt already exists for Event {EventId} and Supplier {SupplierId}",
                receipt.SourceEventId,
                receipt.SupplierId
            );
        }
    }

    public async Task<bool> ExistsAsync(
        string sourceEventId,
        string supplierId,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = TableKeyNormaliser.Normalise(sourceEventId);
        var rowKey = TableKeyNormaliser.Normalise(supplierId);

        try
        {
            var response = await tableClient.GetEntityAsync<WebhookDeliveryReceiptEntity>(
                partitionKey,
                rowKey,
                select: ["RowKey"], // Optimize: we only need to know it exists
                cancellationToken: cancellationToken
            );

            return response.GetRawResponse().Status == 200;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}
