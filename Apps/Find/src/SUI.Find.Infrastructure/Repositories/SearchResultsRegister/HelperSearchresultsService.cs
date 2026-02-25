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

    public async Task<IReadOnlyList<SearchResultsRegisterEntry>> GetResultsByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken
    )
    {
        return await _resultsRepository.GetByJobIdAsync(jobId, cancellationToken);
    }
}
