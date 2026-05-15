using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IJobSearchService
{
    Task<OneOf<SearchResultsV2Dto, NotFound, Forbidden, Error>> GetSearchResultsAsync(
        string workItemId,
        string requestingOrganisationId,
        CancellationToken cancellationToken
    );
}
