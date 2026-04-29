using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Functions.ActivityFunctions;

namespace SUI.Find.FindApi.Functions.OrchestratorFunctions;

public class SearchOrchestrator(ILogger<SearchOrchestrator> logger)
{
    [Function("SearchOrchestrator")]
    public async Task<List<CustodianSearchResultItem>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context
    )
    {
        var data = context.GetInput<SearchOrchestratorInput>();

        if (
            data is null
            || string.IsNullOrWhiteSpace(data.Suid)
            || string.IsNullOrWhiteSpace(data.Metadata.PersonId)
            || string.IsNullOrWhiteSpace(data.PolicyContext.ClientId)
        )
        {
            throw new ArgumentException("Invalid input in Search Orchestrator");
        }

        var jobId = context.InstanceId;

        using var logScope = logger.BeginScope(
            "CorrelationId: {CorrelationId}, JobId: {JobId}",
            data.Metadata.InvocationId,
            jobId
        );

        logger.LogInformation("Search Orchestrator started");

        var availableProviders = await context.CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
            nameof(GetProvidersFunction),
            data.Suid
        );

        if (availableProviders.Count == 0)
        {
            logger.LogWarning("No available providers found");

            return [];
        }

        // Query Providers and apply PEP, via sub-orchestration to ultimately enable partial feedback of results, and the durable way to handle data dependencies for individual work items in a parallel batch.
        var pepFilterTasks = availableProviders
            .Select(provider =>
                context.CallSubOrchestratorAsync<
                    IReadOnlyList<PepResultItem<CustodianSearchResultItem>>
                >(
                    nameof(SearchProviderSubOrchestrator),
                    new SearchProviderSubOrchestratorInput(jobId, data, provider)
                )
            )
            .ToList();

        var pepResultsTaskList = await Task.WhenAll(pepFilterTasks);
        var pepResults = pepResultsTaskList.SelectMany(r => r).ToList();

        logger.LogInformation(
            "{CountOfResults} results after querying all {CountOfProviders} providers and PEP enforcement ({AllowedCount} allowed, {DeniedCount} denied)",
            pepResults.Count,
            availableProviders.Count,
            pepResults.Count(x => x.Decision.IsAllowed),
            pepResults.Count(x => !x.Decision.IsAllowed)
        );

        return pepResults.Where(x => x.Decision.IsAllowed).Select(x => x.Item).ToList();
    }

    [Function(nameof(SearchProviderSubOrchestrator))]
    public async Task<
        IReadOnlyList<PepResultItem<CustodianSearchResultItem>>
    > SearchProviderSubOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<SearchProviderSubOrchestratorInput>();
        ArgumentNullException.ThrowIfNull(input);

        var (jobId, data, provider) = input;
        var requestingOrdId = data.PolicyContext.ClientId;
        var sourceOrgId = provider.OrgId;

        using var logScope = logger.BeginScope(
            "CorrelationId: {CorrelationId}, JobId: {JobId}, RequestingOrdId: {RequestingOrdId}, SourceOrgId: {SourceOrgId}",
            input.SearchInput.Metadata.InvocationId,
            jobId,
            requestingOrdId,
            sourceOrgId
        );

        var options = BuildTaskOptions();

        // Activity One: Query the provider for record pointers
        var providerResults = await context.CallActivityAsync<
            IReadOnlyList<CustodianSearchResultItem>
        >(
            nameof(QueryProvidersFunction),
            new QueryProviderInput(
                requestingOrdId,
                jobId,
                data.Metadata.InvocationId,
                data.Suid,
                provider
            ),
            options
        );

        logger.LogInformation("{Count} results before PEP filtering", providerResults.Count);

        // Activity Two: filter the provider's results based on the requesting provider's data sharing policies (PEP Policy Enforcement Point)
        var filterInput = new PepFilterInput<CustodianSearchResultItem>(
            sourceOrgId,
            requestingOrdId,
            data.PolicyContext.OrgType,
            providerResults,
            provider.DsaPolicy,
            data.PolicyContext.Purpose,
            data.Metadata.InvocationId
        );

        var pepResults = await context.CallActivityAsync<
            IReadOnlyList<PepResultItem<CustodianSearchResultItem>>
        >(nameof(FilterResultsByPolicyFunction), filterInput, options);

        logger.LogInformation(
            "{Count} results after querying provider and PEP enforcement ({AllowedCount} allowed, {DeniedCount} denied)",
            pepResults.Count,
            pepResults.Count(x => x.Decision.IsAllowed),
            pepResults.Count(x => !x.Decision.IsAllowed)
        );

        // Activity Three: Persist the PEP filtered provider's results
        await context.CallActivityAsync(
            nameof(PersistSearchResultsFunction),
            new PersistSearchResultsInput(
                pepResults,
                WorkItemId: jobId, // In this context (fan-out architecture), WorkItemId is the same as JobId.
                InvocationId: data.Metadata.InvocationId,
                JobId: jobId,
                RequestingOrdId: requestingOrdId,
                SourceOrgId: sourceOrgId
            ),
            options
        );

        return pepResults;
    }

    private static TaskOptions BuildTaskOptions() =>
        TaskOptions.FromRetryPolicy(
            new RetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(5),
                backoffCoefficient: 2.0
            )
        );
}
