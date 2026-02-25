using System.Diagnostics.CodeAnalysis;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

[ExcludeFromCodeCoverage(Justification = "Used only as a helper with no logic embedded")]
public class HelperSearchresultsService : ISearchResultsService
{
    private readonly ISearchResultEntryRepository _resultsRepository;

    public HelperSearchresultsService(ISearchResultEntryRepository resultsRepository)
    {
        _resultsRepository = resultsRepository;
    }

    public async Task<IReadOnlyList<SearchResultEntry>> GetResultsByWorkItemIdAsync(
        string workItemId,
        CancellationToken cancellationToken
    )
    {
        return await _resultsRepository.GetByWorkItemIdAsync(workItemId, cancellationToken);
    }
}
