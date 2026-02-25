using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Interfaces;

public interface ISearchResultsService
{
    Task<IReadOnlyList<SearchResultEntry>> GetResultsByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken
    );
}
