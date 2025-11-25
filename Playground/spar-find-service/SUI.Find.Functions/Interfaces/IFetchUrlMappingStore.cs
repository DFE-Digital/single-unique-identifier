using Models;

namespace Interfaces;

public interface IFetchUrlMappingStore
{
    Task<MaskedUrl> CreateAsync(
        string jobId,
        string targetUrl,
        string orgId,
        string recordType,
        TimeSpan ttl,
        CancellationToken ct);

    Task<string?> ResolveAsync(string jobId, string fetchId, CancellationToken ct);
}