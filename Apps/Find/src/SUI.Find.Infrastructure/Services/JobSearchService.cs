using System.Text.Json;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;
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

        if (
            !ValidateJobCountEntity(
                workItemId,
                searchingOrganisationId,
                workItemJobCountEntity,
                out var searchResultsAsync
            )
        )
            return searchResultsAsync!.Value;

        var completedRecords = await searchResultsEntryRepository.GetByWorkItemIdAsync(
            workItemId,
            cancellationToken
        );

        var completenessPercentage =
            completedRecords.Count * 100 / workItemJobCountEntity!.ExpectedJobCount;

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

    private bool ValidateJobCountEntity(
        string workItemId,
        string searchingOrganisationId,
        WorkItemJobCount? workItemJobCountEntity,
        out OneOf<SearchResultsV2Dto, NotFound, Unauthorized, Error>? searchResultsAsync
    )
    {
        searchResultsAsync = null;

        if (workItemJobCountEntity == null)
        {
            logger.LogInformation(
                "No work item job count found for work item ID {workItemId}",
                workItemId
            );
            searchResultsAsync = new NotFound();
            return false;
        }

        if (workItemJobCountEntity.SearchingOrganisationId != searchingOrganisationId)
        {
            logger.LogWarning(
                "Searcher attempted to access an unauthorized work item. Searching ID: {searchingId}, expected ID: {expectedId}",
                searchingOrganisationId,
                workItemJobCountEntity.SearchingOrganisationId
            );
            searchResultsAsync = new Unauthorized();
            return false;
        }

        if (workItemJobCountEntity.ExpectedJobCount == 0)
        {
            logger.LogInformation("No jobs found for work item ID {workItemId}", workItemId);
            searchResultsAsync = new NotFound();
            return false;
        }

        return true;
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
