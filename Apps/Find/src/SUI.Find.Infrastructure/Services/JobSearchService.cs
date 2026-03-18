using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Configuration;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Repositories.SearchResultEntryStorage;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.Services;

public class JobSearchService(
    SearchResultEntryRepository searchResultsEntryRepository,
    WorkItemJobCountRepository workItemJobCountRepository,
    TimeProvider timeProvider,
    IOptionsMonitor<JobClaimConfig> options,
    ILogger<JobSearchService> logger
) : IJobSearchService
{
    public async Task<
        OneOf<SearchResultsV2Dto, NotFound, Unauthorized, Error>
    > GetSearchResultsAsync(string workItemId, CancellationToken cancellationToken)
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

        var completenessPercentage = completedRecords.Count / totalJobs;

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
        };

        return result;
    }

    private SearchStatus GetOverallJobStatus(
        int completenessPercentage,
        WorkItemJobCount workItemJobCount
    )
    {
        if (completenessPercentage < 100)
            return SearchStatus.Running;

        return
            workItemJobCount.CreatedAtUtc
            < timeProvider
                .GetUtcNow()
                .AddHours(-1 * options.CurrentValue.AvailableJobWindowStartOffsetHours)
            ? SearchStatus.Expired
            : SearchStatus.Completed;
    }
}
