using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;

namespace SUI.Find.Infrastructure.Services;

/// <summary>
/// Uses Azure Table Storage to write audit logs
/// </summary>
public interface IStorageTableAuditService : IAuditService
{
    Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken);
}

[ExcludeFromCodeCoverage(
    Justification = "Infrastructure - covered by integration tests not yet implemented"
)]
public class StorageTableAuditService(TableServiceClient client) : IStorageTableAuditService
{
    public async Task WriteAccessAuditLogAsync(
        string orgId,
        string url,
        string method,
        DateTime accessTime,
        string correlationId
    )
    {
        await Write(orgId, url, method, string.Empty, accessTime, correlationId);
    }

    public Task WriteSearchWithSuidAuditLogAsync(
        string orgId,
        string url,
        string method,
        string? hashedValue,
        DateTime accessTime,
        string correlationId
    )
    {
        return Write(orgId, url, method, hashedValue, accessTime, correlationId);
    }

    private async Task Write(
        string orgId,
        string url,
        string method,
        string? data,
        DateTime accessTime,
        string correlationId
    )
    {
        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableAudit.TableName
        );
        var partitionKey = $"{orgId}_{accessTime:yyyyMM}"; // Partition by orgId to avoid hot partitions, and by month for easier querying
        await tableClient.AddEntityAsync(
            new TableEntity(partitionKey, Guid.NewGuid().ToString())
            {
                { "OrganisationId", orgId },
                { "UrlAccessed", url },
                { "Method", method },
                { "AccessTime", accessTime.ToUniversalTime() },
                { "CorrelationId", correlationId },
                { "Suid", data },
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
