using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.Infrastructure.Services;

public class AuditLogService(
    ILogger<AuditLogService> logger,
    BlobContainerClient blobContainerClient
) : IAuditLogService
{
    public async Task LogIncomingRequestAsync(
        string callerId,
        string correlationId,
        DateTimeOffset timestamp,
        string httpMethod,
        string requestPath,
        object? requestData,
        CancellationToken cancellationToken = default
    )
    {
        var entry = new AuditLogEntry
        {
            EventType = "GetAnIdentifierIncomingRequest",
            CallerId = callerId,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            HttpMethod = httpMethod,
            RequestPath = requestPath,
            RequestBody = requestData,
        };

        await WriteAuditLogAsync(entry, cancellationToken);
    }

    public async Task LogOutgoingResponseAsync(
        string callerId,
        string correlationId,
        DateTimeOffset timestamp,
        int statusCode,
        string responseSummary,
        CancellationToken cancellationToken = default
    )
    {
        var entry = new AuditLogEntry
        {
            EventType = "GetAnIdentifierOutgoingResponse",
            CallerId = callerId,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            StatusCode = statusCode,
            ResponseSummary = responseSummary,
        };

        await WriteAuditLogAsync(entry, cancellationToken);
    }

    private async Task WriteAuditLogAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        // 1. Log via ILogger (Application Insights receives structured telemetry properties)
        logger.LogInformation(
            "AuditLog [{EventType}] - CallerId: {CallerId}, CorrelationId: {CorrelationId}, Timestamp: {Timestamp}, HttpMethod: {HttpMethod}, RequestPath: {RequestPath}, StatusCode: {StatusCode}, ResponseSummary: {ResponseSummary}",
            entry.EventType,
            entry.CallerId,
            entry.CorrelationId,
            entry.Timestamp.ToString("s"),
            entry.HttpMethod,
            entry.RequestPath,
            entry.StatusCode,
            entry.ResponseSummary
        );

        // 2. Persist to Blob Storage
        try
        {
            await blobContainerClient
                .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var datePath = entry.Timestamp.ToString("yyyy/MM/dd");
            var blobName =
                $"{datePath}/{entry.CorrelationId}_{entry.EventType}_{entry.Timestamp.Ticks}.json";
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            var json = JsonSerializer.Serialize(entry);

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to write audit log to blob storage for correlation ID {CorrelationId}",
                entry.CorrelationId
            );
        }
    }
}
