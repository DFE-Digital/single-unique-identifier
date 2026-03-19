using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Configuration;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.Services;

public class JobSearchService(
    ISearchResultEntryRepository searchResultsEntryRepository,
    IWorkItemJobCountRepository workItemJobCountRepository,
    TimeProvider timeProvider,
    IOptionsMonitor<JobClaimConfig> options,
    ILogger<JobSearchService> logger
) : IJobSearchService
{
    public async Task<
        OneOf<SearchResultsV2Dto, NotFound, Unauthorized, Error>
    > GetSearchResultsAsync(
        string workItemId,
        string searchingOrganisationId,
        CancellationToken cancellationToken
    )
    {
        var workItemJobCountEntity =
            await workItemJobCountRepository.GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                cancellationToken
            );

        if (workItemJobCountEntity == null)
        {
            logger.LogInformation(
                "No work item job count found for work item ID {workItemId}",
                workItemId
            );
            return new NotFound();
        }

        if (workItemJobCountEntity.SearchingOrganisationId != searchingOrganisationId)
        {
            logger.LogWarning(
                "Searcher attempted to access an unauthorized work item. Searching ID: {searchingId}, expected ID: {expectedId}",
                searchingOrganisationId,
                workItemJobCountEntity.SearchingOrganisationId
            );
            return new Unauthorized();
        }

        var totalJobs = workItemJobCountEntity.ExpectedJobCount;

        if (totalJobs == 0)
        {
            logger.LogInformation("No jobs found for work item ID {workItemId}", workItemId);
            return new NotFound();
        }

        var completedRecords = await searchResultsEntryRepository.GetByWorkItemIdAsync(
            workItemId,
            cancellationToken
        );

        var completenessPercentage = (completedRecords.Count * 100 / totalJobs);

        var status = GetOverallJobStatus(completenessPercentage, workItemJobCountEntity);

        var payload = JsonSerializer.Deserialize<CustodianLookupPayload>(
            workItemJobCountEntity.PayloadJson,
            JsonSerializerOptions.Web
        );

        var result = new SearchResultsV2Dto
        {
            WorkItemId = workItemId,
            Suid = payload?.Sui ?? string.Empty,
            Status = status,
            Items = completedRecords,
            CompletenessPercentage = completenessPercentage,
        };

        return result;
    }

    private SearchStatus GetOverallJobStatus(
        int completenessPercentage,
        WorkItemJobCount workItemJobCount
    )
    {
        if (
            workItemJobCount.CreatedAtUtc
            < timeProvider
                .GetUtcNow()
                .AddHours(-1 * options.CurrentValue.AvailableJobWindowStartOffsetHours)
        )
            return SearchStatus.Expired;

        return completenessPercentage < 100 ? SearchStatus.Running : SearchStatus.Completed;
    }
}
