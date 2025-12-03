using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Uses TableServiceClient directly")]
public class UrlStorageTableService(TableServiceClient client)
    : IFetchUrlStorageService,
        ITableServiceEnsureCreated
{
    public async Task AddAsync(AddFetchUrlRequest request, CancellationToken ct)
    {
        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableUrlMappings.TableName
        );

        var partitionKey = request.JobId[..2]; // Simple partitioning: first 2 chars of id
        var expiresAt = DateTimeOffset.UtcNow.Add(request.Ttl);

        var entity = new FetchUrlMappingEntity
        {
            PartitionKey = partitionKey,
            RowKey = request.FetchId,
            Timestamp = DateTimeOffset.Now,
            TargetUrl = request.TargetUrl,
            ExpiresAtUtc = expiresAt,
            TargetOrgId = request.TargetOrg,
            RequestingOrgId = request.RequestingOrg,
            RecordType = request.RecordType,
            JobId = request.JobId,
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
