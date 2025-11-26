using Models;

namespace Interfaces;

public interface IFetchUrlMappingStore
{
    Task<MaskedUrl> CreateAsync(
        string jobId,
        string targetUrl,
        string targetOrg,
        string requestingOrg,
        string recordType,
        TimeSpan ttl,
        CancellationToken ct);

    Task<ResolvedFetchMapping?> ResolveAsync(
        string requestingOrg, 
        string fetchId,
        CancellationToken ct);
}