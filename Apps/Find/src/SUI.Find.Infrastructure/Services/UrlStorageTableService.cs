using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public interface IUrlStorageTableService
{
    Task AddAsync(
        string jobId,
        string targetUrl,
        string targetOrg,
        string requestingOrg,
        string recordType,
        TimeSpan ttl,
        CancellationToken ct
    );
}

[ExcludeFromCodeCoverage(Justification = "Uses TableServiceClient directly")]
public class UrlStorageTableService(TableServiceClient client)
    : IUrlStorageTableService,
        ITableServiceEnsureCreated
{
    public async Task AddAsync(
        string jobId,
        string targetUrl,
        string targetOrg,
        string requestingOrg,
        string recordType,
        TimeSpan ttl,
        CancellationToken ct
    )
    {
        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableUrlMappings.TableName
        );

        var partitionKey = jobId[..2]; // Simple partitioning: first 2 chars of id
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        var entity = new FetchUrlMappingEntity
        {
            PartitionKey = partitionKey,
            RowKey = jobId,
            Timestamp = DateTimeOffset.Now,
            TargetUrl = targetUrl,
            ExpiresAtUtc = expiresAt,
            TargetOrgId = targetOrg,
            RequestingOrgId = requestingOrg,
            RecordType = recordType,
            JobId = jobId,
        };

        await tableClient.AddEntityAsync(entity, ct);
    }

    public async Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken)
    {
        await client.CreateTableIfNotExistsAsync(
            InfrastructureConstants.StorageTableUrlMappings.TableName,
            cancellationToken
        );
    }
}
