using SUI.Find.Application.Models;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public interface ISearchResultsRegisterRepository
{
    Task AddAsync(SearchResultsRegisterEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultsRegisterEntry>> GetByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken
    );
}
