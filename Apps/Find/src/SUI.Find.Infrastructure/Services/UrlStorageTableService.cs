using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
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

        var partitionKey = request.RequestingOrg[..5]; // Simple partitioning: first 5 chars of id
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

    public async Task<ResolvedFetchMappingResult> GetAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    )
    {
        try
        {
            var partitionKey = requestingOrg[..5];
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
                return new ResolvedFetchMappingResult.Expired();
            }

            if (res.Value.RequestingOrgId != requestingOrg)
            {
                logger.LogWarning(
                    "Requesting organisation {RequestingOrg} does not match for fetch ID {FetchId}",
                    requestingOrg,
                    fetchId
                );
                return new ResolvedFetchMappingResult.Unauthorized();
            }

            return new ResolvedFetchMappingResult.Success(
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
            return new ResolvedFetchMappingResult.Fail();
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
