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
        string orgId,
        string recordType,
        TimeSpan ttl,
        CancellationToken ct)
    {
        await _table.CreateIfNotExistsAsync(ct);

        var fetchId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(ttl);

        var entity = new FetchUrlMappingEntity
        {
            PartitionKey = jobId,
            RowKey = fetchId,
            TargetUrl = targetUrl,
            ExpiresAtUtc = expiresAt,
            OrgId = orgId,
            RecordType = recordType
        };

        await _table.AddEntityAsync(entity, ct);

        var maskedUrl = $"/v1/fetch/{fetchId}";

        return new MaskedUrl(fetchId, maskedUrl, expiresAt);
    }

    public async Task<string?> ResolveAsync(string jobId, string fetchId, CancellationToken ct)
    {
        try
        {
            var res = await _table.GetEntityAsync<FetchUrlMappingEntity>(jobId, fetchId, cancellationToken: ct);

            if (res.Value.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                return null;

            return res.Value.TargetUrl;
        }
        catch
        {
            return null;
        }
    }
}
