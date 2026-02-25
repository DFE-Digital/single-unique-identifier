using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Interfaces;

public interface ISearchResultsService
{
    Task<IReadOnlyList<SearchResultEntry>> GetResultsByWorkItemIdAsync(
        string workItemId,
        CancellationToken cancellationToken
    );
}
