using SUI.Find.Application.Dtos;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public interface ISearchResultEntryRepository
{
    Task UpsertAsync(SearchResultEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultEntry>> GetByWorkItemIdAsync(
        string workItemId,
        CancellationToken cancellationToken
    );
}
