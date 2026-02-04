using OneOf;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IMaskUrlService
{
    Task<IReadOnlyList<CustodianSearchResultItem>> CreateAsync(
        List<CustodianSearchResultItem> items,
        QueryProviderInput input,
        CancellationToken ct
    );

    Task<OneOf<ResolvedFetchMapping, NotFound, Unauthorized, Error>> ResolveAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    );
}
