using SUI.Find.Application.Dtos;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public interface ISearchResultEntryRepository
{
    Task UpsertAsync(SearchResultsRegisterEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultsRegisterEntry>> GetByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken
    );
}
