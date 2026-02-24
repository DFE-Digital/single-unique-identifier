using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Interfaces;

public interface ISearchResultsService
{
    Task<IReadOnlyList<SearchResultsRegisterEntry>> GetResultsByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken
    );
}
