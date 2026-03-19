using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IJobSearchService
{
    Task<OneOf<SearchResultsV2Dto, NotFound, Unauthorized, Error>> GetSearchResultsAsync(
        string workItemId,
        string searchingOrganisationId,
        CancellationToken cancellationToken
    );
}
