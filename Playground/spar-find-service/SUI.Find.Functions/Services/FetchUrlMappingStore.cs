using Azure.Data.Tables;
using Models;
using Interfaces;

public sealed class FetchUrlMappingStore : IFetchUrlMappingStore
{
    private const string TableName = "FetchUrlMappings";
    private readonly TableClient _table;

    public FetchUrlMappingStore(TableServiceClient svc)
    {
        _table = svc.GetTableClient(TableName);
        _table.CreateIfNotExists();
    }

    public async Task<MaskedUrl> CreateAsync(
        string jobId,
        string targetUrl,
        string targetOrg,
        string requestingOrg, 
        string recordType,
        TimeSpan ttl,
        CancellationToken ct)
    {
        await _table.CreateIfNotExistsAsync(ct);

        var fetchId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        // Simple partitioning: first 2 chars of fetchId
        var partitionKey = fetchId[..2];

        var entity = new FetchUrlMappingEntity
        {
            PartitionKey = partitionKey,
            RowKey = fetchId,
            TargetUrl = targetUrl,
            ExpiresAtUtc = expiresAt,
            TargetOrgId = targetOrg,
            RequestingOrgId = requestingOrg,
            RecordType = recordType,
            JobId = jobId
        };

        await _table.AddEntityAsync(entity, ct);

        var maskedUrl = $"/v1/records/{fetchId}";

        return new MaskedUrl(fetchId, maskedUrl, expiresAt);
    }

    public async Task<ResolvedFetchMapping?> ResolveAsync(string requestingOrg, string fetchId,CancellationToken ct)
    { 
        try
        {
            var partitionKey = fetchId[..2];

            var res = await _table.GetEntityAsync<FetchUrlMappingEntity>(
                partitionKey,
                fetchId,
                cancellationToken: ct);

            // Make sure the url is still valie
            if (res.Value.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            // Only the organisation that triggered the creation of the url can use it
            if (res.Value.RequestingOrgId != requestingOrg)
            {
                return null;
            }

            return new ResolvedFetchMapping(
                TargetUrl: res.Value.TargetUrl,
                TargetOrgId: res.Value.TargetOrgId,
                RecordType: res.Value.RecordType);  
        }
        catch
        {
            return null;
        }
    }
}
