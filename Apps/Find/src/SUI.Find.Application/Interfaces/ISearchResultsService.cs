using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.Application.Interfaces;

public interface ISearchResultsService
{
    Task<int> PersistSearchResultsAsync(
        string workItemId,
        string jobId,
        IReadOnlyCollection<PepResultItem<CustodianSearchResultItem>> searchResults,
        CancellationToken cancellationToken
    );
}
