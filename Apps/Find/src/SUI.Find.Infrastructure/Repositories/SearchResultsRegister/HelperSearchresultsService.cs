using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public class HelperSearchresultsService : ISearchResultsService
{
    private readonly ISearchResultsRegisterRepository _resultsRepository;

    public HelperSearchresultsService(ISearchResultsRegisterRepository resultsRepository)
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
