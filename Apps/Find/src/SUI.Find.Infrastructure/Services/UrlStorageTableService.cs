using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Uses TableServiceClient directly")]
public class UrlStorageTableService(
    ILogger<UrlStorageTableService> logger,
    TableServiceClient client
) : IFetchUrlStorageService, ITableServiceEnsureCreated
{
    public async Task AddAsync(AddFetchUrlRequest request, CancellationToken ct)
    {
        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableUrlMappings.TableName
        );

        var partitionKey = request.JobId[..5]; // Simple partitioning: first 2 chars of id
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

    public async Task<Result<ResolvedFetchMapping>> GetAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    )
    {
        try
        {
            var partitionKey = fetchId[..2];
            var tableClient = client.GetTableClient(
                InfrastructureConstants.StorageTableUrlMappings.TableName
            );

            var res = await tableClient.GetEntityAsync<FetchUrlMappingEntity>(
                partitionKey,
                fetchId,
                cancellationToken: ct
            );

            if (res.Value.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                logger.LogWarning("Fetch URL {FetchId} has expired", fetchId);
                return Result<ResolvedFetchMapping>.Fail("Fetch URL has expired");
            }

            if (res.Value.RequestingOrgId != requestingOrg)
            {
                logger.LogWarning(
                    "Requesting organisation {RequestingOrg} does not match for fetch ID {FetchId}",
                    requestingOrg,
                    fetchId
                );
                return Result<ResolvedFetchMapping>.Fail("Requesting organisation does not match");
            }

            return Result<ResolvedFetchMapping>.Ok(
                new ResolvedFetchMapping(
                    TargetUrl: res.Value.TargetUrl,
                    TargetOrgId: res.Value.TargetOrgId,
                    RecordType: res.Value.RecordType
                )
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving fetch URL mapping for FetchId {FetchId}",
                fetchId
            );
            return Result<ResolvedFetchMapping>.Fail("Failed to retrieve fetch URL mapping");
        }
    }

    public async Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken)
    {
        await client.CreateTableIfNotExistsAsync(
            InfrastructureConstants.StorageTableUrlMappings.TableName,
            cancellationToken
        );
    }
}
