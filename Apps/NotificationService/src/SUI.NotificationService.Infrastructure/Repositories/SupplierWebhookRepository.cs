using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Infrastructure.Entities;
using SUI.NotificationService.Infrastructure.Utilities;

namespace SUI.NotificationService.Infrastructure.Repositories;

public class SupplierWebhookRepository(
    TableClient tableClient,
    ILogger<SupplierWebhookRepository> logger
) : ISupplierWebhookRepository
{
    public async Task AddAsync(
        SupplierWebhook webhook,
        CancellationToken cancellationToken = default
    )
    {
        var entity = MapToEntity(webhook);

        try
        {
            await tableClient.AddEntityAsync(entity, cancellationToken);
            logger.LogInformation(
                "Successfully added webhook register for SupplierId: {SupplierId}",
                webhook.SupplierId
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 409) // 409 Conflict natively prevents duplicates
        {
            logger.LogWarning(
                "Attempted to add duplicate webhook register for SupplierId: {SupplierId}",
                webhook.SupplierId
            );
            throw new InvalidOperationException(
                $"A webhook register for Supplier '{webhook.SupplierId}' already exists."
            );
        }
    }

    public async Task UpdateAsync(
        SupplierWebhook webhook,
        CancellationToken cancellationToken = default
    )
    {
        var entity = MapToEntity(webhook);

        await tableClient.UpdateEntityAsync(
            entity,
            ETag.All,
            TableUpdateMode.Replace,
            cancellationToken
        );
        logger.LogInformation(
            "Successfully updated webhook register for SupplierId: {SupplierId}",
            webhook.SupplierId
        );
    }

    public async Task DisableAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        var normalisedId = TableKeyNormaliser.Normalise(supplierId);

        try
        {
            var response = await tableClient.GetEntityAsync<SupplierWebhookEntity>(
                SupplierWebhookEntity.DefaultPartitionKey,
                normalisedId,
                cancellationToken: cancellationToken
            );

            var entity = response.Value;
            entity.IsEnabled = false;

            await tableClient.UpdateEntityAsync(
                entity,
                entity.ETag,
                TableUpdateMode.Replace,
                cancellationToken
            );
            logger.LogInformation(
                "Successfully disabled webhook register for SupplierId: {SupplierId}",
                supplierId
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            logger.LogWarning(
                "Attempted to disable non-existent webhook register for SupplierId: {SupplierId}",
                supplierId
            );
            throw new KeyNotFoundException(
                $"Webhook register for Supplier '{supplierId}' was not found."
            );
        }
    }

    public async Task<IEnumerable<SupplierWebhook>> GetAllEnabledAsync(
        CancellationToken cancellationToken = default
    )
    {
        var webhooks = new List<SupplierWebhook>();

        // Filters purely on active endpoints for fast retrieval
        var query = tableClient.QueryAsync<SupplierWebhookEntity>(
            filter: $"PartitionKey eq '{SupplierWebhookEntity.DefaultPartitionKey}' and IsEnabled eq true",
            cancellationToken: cancellationToken
        );

        await foreach (var entity in query)
        {
            webhooks.Add(MapToDomain(entity));
        }

        return webhooks;
    }

    private static SupplierWebhookEntity MapToEntity(SupplierWebhook domain) =>
        new()
        {
            // Normalise the RowKey so Azure accepts it
            RowKey = TableKeyNormaliser.Normalise(domain.SupplierId),

            // Preserve the original unmutated ID for data fidelity
            OriginalSupplierId = domain.SupplierId,

            EndpointUrl = domain.EndpointUrl,
            IsEnabled = domain.IsEnabled,
            ContractVersion = domain.ContractVersion,
            SecretKeyVaultReference = domain.SecretKeyVaultReference,
        };

    private static SupplierWebhook MapToDomain(SupplierWebhookEntity entity) =>
        new(
            // Rehydrate the domain model using the original, unmutated ID
            entity.OriginalSupplierId,
            entity.EndpointUrl,
            entity.IsEnabled,
            entity.ContractVersion,
            entity.SecretKeyVaultReference
        );
}
