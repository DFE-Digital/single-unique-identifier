using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.Application.Services;

public class SearchResultsService(
    ISearchResultEntryRepository searchResultEntryRepository,
    ILogger<SearchResultsService> logger
) : ISearchResultsService
{
    public async Task<int> PersistSearchResultsAsync(
        string workItemId,
        string jobId,
        IReadOnlyCollection<SearchResultWithDecision> searchResults,
        CancellationToken cancellationToken
    )
    {
        var submittedAtUtc = DateTimeOffset.UtcNow;
        var countOfRecordsPersisted = 0;

        foreach (
            var searchResultItem in searchResults
                .Where(x => x.Decision.IsAllowed)
                .Select(x => x.Item)
        )
        {
            await searchResultEntryRepository.UpsertAsync(
                new SearchResultEntry
                {
                    CustodianId = searchResultItem.CustodianId,
                    SystemId = searchResultItem.SystemId,
                    CustodianName = searchResultItem.CustodianName,
                    RecordType = searchResultItem.RecordType,
                    RecordUrl = searchResultItem.RecordUrl,
                    RecordId = searchResultItem.RecordId,
                    SubmittedAtUtc = submittedAtUtc,
                    JobId = jobId,
                    WorkItemId = workItemId,
                },
                cancellationToken
            );
            countOfRecordsPersisted++;
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "{Count} results persisted after PEP filtering",
                countOfRecordsPersisted
            );

        return countOfRecordsPersisted;
    }
}
