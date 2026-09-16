using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using SUI.GetAnIdentifier.Infrastructure.Interfaces;
using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.Infrastructure.Services;

public class AuditService(ILogger<AuditService> logger, BlobContainerClient blobContainerClient)
    : IAuditService
{
    public async Task SendAuditEventAsync(AuditEvent entry)
    {
        var auditEntryString = JsonSerializer.Serialize(entry);

        // 1. Log via ILogger
        logger.LogInformation(auditEntryString);

        // 2. Persist to Blob Storage
        try
        {
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            var datePath = entry.Timestamp.ToString("yyyy/MM/dd");
            var blobName = $"{datePath}/{entry.CorrelationId}_{entry.Timestamp.Ticks}.json";
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(auditEntryString)
            );
            await blobClient.UploadAsync(stream, overwrite: true);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(
                ex,
                "Error occurred in azure services while attempting to write audit logs. Correlation ID: {CorrelationId}",
                entry.CorrelationId
            );
            throw;
        }
        catch (NotSupportedException ex)
        {
            logger.LogError(
                ex,
                "Error occurred during JSON serialization of audit entry. Correlation ID: {CorrelationId}",
                entry.CorrelationId
            );
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to write audit log to blob storage. Correlation ID: {CorrelationId}",
                entry.CorrelationId
            );
            throw;
        }
    }
}
