using SUI.Find.Application.Models;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public interface ISearchResultsRegisterRepository
{
    Task AddAsync(
        string jobId,
        CustodianSearchResultItem item,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<SearchResultsRegisterEntry>> GetByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken
    );
}
