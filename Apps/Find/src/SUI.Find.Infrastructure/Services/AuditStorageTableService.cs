using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using SUI.Find.Application.Interfaces;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Basic Infrastructure code.")]
public class AuditStorageTableService(TableServiceClient client) : ITableStorageAuditService
{
    public async Task WriteAccessAuditLogAsync(
        string clientId,
        string url,
        string method,
        DateTime accessTime,
        string correlationId
    )
    {
        await Write(clientId, url, method, string.Empty, accessTime, correlationId);
    }

    public Task WriteAccessWithSuidAuditLogAsync(
        string clientId,
        string url,
        string method,
        string? suid,
        DateTime accessTime,
        string correlationId
    )
    {
        return Write(clientId, url, method, suid, accessTime, correlationId);
    }

    private async Task Write(
        string clientId,
        string url,
        string method,
        string? suid,
        DateTime accessTime,
        string correlationId
    )
    {
        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableAudit.TableName
        );

        // Partition by clientId to avoid hot partitions, and by month for easier querying
        var partitionKey = $"{clientId}_{accessTime:yyyyMM}";
        await tableClient.AddEntityAsync(
            new TableEntity(partitionKey, Guid.NewGuid().ToString())
            {
                { "ClientId", clientId },
                { "UrlAccessed", url },
                { "Method", method },
                { "AccessTime", accessTime.ToUniversalTime() },
                { "CorrelationId", correlationId },
                { "Suid", suid },
            }
        );
    }

    public async Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken)
    {
        await client.CreateTableIfNotExistsAsync(
            InfrastructureConstants.StorageTableAudit.TableName,
            cancellationToken
        );
    }
}
