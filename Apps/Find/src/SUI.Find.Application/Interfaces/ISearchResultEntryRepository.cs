using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Interfaces;

public interface ISearchResultEntryRepository
{
    Task UpsertAsync(SearchResultEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultEntry>> GetByWorkItemIdAsync(
        string workItemId,
        string requestingOrganisationId,
        CancellationToken cancellationToken
    );
}
