using System.Text.Json;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.Services;

public class JobSearchService(
    ISearchResultEntryRepository searchResultsEntryRepository,
    IWorkItemJobCountRepository workItemJobCountRepository,
    IJobWindowStartService jobWindowStartService,
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

        if (workItemJobCountEntity == null || workItemJobCountEntity.ExpectedJobCount == 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("No jobs found for work item ID {WorkItemId}", workItemId);
            return new NotFound();
        }

        if (workItemJobCountEntity.SearchingOrganisationId != searchingOrganisationId)
        {
            logger.LogWarning(
                "Searching organisation ID ({SearchingOrganisationId}) from request does not match organisation ID ({ExpectedSearchingOrganisationId}) on work item. Work item ID: {WorkItemId}",
                searchingOrganisationId,
                workItemJobCountEntity.SearchingOrganisationId,
                workItemId
            );
            return new Unauthorized();
        }

        var completedRecords = await searchResultsEntryRepository.GetByWorkItemIdAsync(
            workItemId,
            searchingOrganisationId,
            cancellationToken
        );

        var completedJobCount = completedRecords.DistinctBy(record => record.JobId).Count();

        var completenessPercentage =
            completedJobCount * 100 / workItemJobCountEntity.ExpectedJobCount;

        var status = GetOverallJobStatus(completenessPercentage, workItemJobCountEntity);

        var payload = JsonSerializer.Deserialize<CustodianLookupJobPayload>(
            workItemJobCountEntity.PayloadJson,
            JsonSerializerOptions.Web
        );

        var result = new SearchResultsV2Dto
        {
            WorkItemId = workItemId,
            Suid = payload?.Sui ?? string.Empty,
            Status = status,
            Items = completedRecords.Select(SearchResultItem (x) => x).ToArray(),
            CompletenessPercentage = completenessPercentage,
        };

        return result;
    }

    private SearchStatus GetOverallJobStatus(
        int completenessPercentage,
        WorkItemJobCount workItemJobCount
    )
    {
        if (completenessPercentage >= 100)
            return SearchStatus.Completed;

        return workItemJobCount.CreatedAtUtc < jobWindowStartService.GetWindowStart()
            ? SearchStatus.Expired
            : SearchStatus.Running;
    }
}
