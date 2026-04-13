using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public sealed record PersistSearchResultsInput(
    IReadOnlyList<SearchResultWithDecision> SearchResults,
    string WorkItemId,
    string JobId,
    string InvocationId,
    string RequestingOrdId,
    string SourceOrgId
);

public class PersistSearchResultsFunction(
    ISearchResultsService searchResultsService,
    ILogger<PersistSearchResultsFunction> logger
)
{
    [Function(nameof(PersistSearchResultsFunction))]
    public async Task PersistSearchResults(
        [ActivityTrigger] PersistSearchResultsInput input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        using var logScope = logger.BeginScope(
            "CorrelationId: {CorrelationId}, Context: {Context}",
            input.InvocationId,
            new
            {
                input.WorkItemId,
                input.JobId,
                input.RequestingOrdId,
                input.SourceOrgId,
            }
        );
        logger.LogInformation($"{nameof(PersistSearchResults)} triggered");

        await searchResultsService.PersistSearchResultsAsync(
            workItemId: input.WorkItemId,
            jobId: input.JobId,
            input.SearchResults,
            cancellationToken
        );
    }
}
