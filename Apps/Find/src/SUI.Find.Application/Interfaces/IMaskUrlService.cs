using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IMaskUrlService
{
    Task<IReadOnlyList<SearchResultItem>> CreateAsync(
        List<SearchResultItem> items,
        QueryProviderInput input,
        CancellationToken ct
    );

    Task<Result<ResolvedFetchMapping>> ResolveAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    );
}
