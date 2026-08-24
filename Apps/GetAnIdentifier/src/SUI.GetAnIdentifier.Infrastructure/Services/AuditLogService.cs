using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Infrastructure.Configuration;

namespace SUI.GetAnIdentifier.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly AuditStorageOptions _options;
    private readonly BlobContainerClient? _blobContainerClient;

    public AuditLogService(
        ILogger<AuditLogService> logger,
        IOptions<AuditStorageOptions> options,
        IConfiguration configuration
    )
        : this(logger, options, configuration, null) { }

    public AuditLogService(
        ILogger<AuditLogService> logger,
        IOptions<AuditStorageOptions> options,
        IConfiguration configuration,
        BlobContainerClient? blobContainerClient
    )
    {
        _logger = logger;
        _options = options.Value;

        if (blobContainerClient != null)
        {
            _blobContainerClient = blobContainerClient;
            return;
        }

        var connectionString = !string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? _options.ConnectionString
            : configuration["AzureWebJobsStorage"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var containerName = string.IsNullOrWhiteSpace(_options.ContainerName)
                    ? "audit-logs"
                    : _options.ContainerName;
                _blobContainerClient = new BlobContainerClient(connectionString, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Audit Log BlobContainerClient");
            }
        }
    }

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
        _logger.LogInformation(
            "AuditLog [{EventType}] - CallerId: {CallerId}, CorrelationId: {CorrelationId}, Timestamp: {Timestamp}, HttpMethod: {HttpMethod}, RequestPath: {RequestPath}, StatusCode: {StatusCode}, ResponseSummary: {ResponseSummary}",
            entry.EventType,
            entry.CallerId,
            entry.CorrelationId,
            entry.Timestamp,
            entry.HttpMethod,
            entry.RequestPath,
            entry.StatusCode,
            entry.ResponseSummary
        );

        // 2. Persist to Blob Storage if container client is configured
        if (_blobContainerClient != null)
        {
            try
            {
                await _blobContainerClient
                    .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var datePath = entry.Timestamp.ToString("yyyy/MM/dd");
                var blobName =
                    $"{datePath}/{entry.CorrelationId}_{entry.EventType}_{entry.Timestamp.Ticks}.json";
                var blobClient = _blobContainerClient.GetBlobClient(blobName);

                var json = JsonSerializer.Serialize(
                    entry,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to write audit log to blob storage for correlation ID {CorrelationId}",
                    entry.CorrelationId
                );
            }
        }
    }
}
