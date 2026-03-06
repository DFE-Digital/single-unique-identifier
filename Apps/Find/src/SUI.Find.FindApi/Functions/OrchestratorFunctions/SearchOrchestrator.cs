using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.FindApi.Functions.ActivityFunctions;

namespace SUI.Find.FindApi.Functions.OrchestratorFunctions;

public class SearchOrchestrator(ILogger<SearchOrchestrator> logger)
{
    [Function("SearchOrchestrator")]
    public async Task<List<CustodianSearchResultItem>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context
    )
    {
        var options = TaskOptions.FromRetryPolicy(
            new RetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(5),
                backoffCoefficient: 2.0
            )
        );

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
            "GetProvidersFunction",
            data.Suid
        );

        if (availableProviders.Count == 0)
        {
            logger.LogWarning("No available providers found");

            return new List<CustodianSearchResultItem>();
        }

        var queryProviderTasks = new List<Task<IReadOnlyList<CustodianSearchResultItem>>>(
            availableProviders.Count
        );

        foreach (var provider in availableProviders)
        {
            queryProviderTasks.Add(
                context.CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                    "QueryProvidersFunction",
                    new QueryProviderInput(
                        data.PolicyContext.ClientId,
                        jobId,
                        data.Metadata.InvocationId,
                        data.Suid,
                        provider
                    ),
                    options
                )
            );
        }

        var queryProviderTaskResultsList = await Task.WhenAll(queryProviderTasks);

        var aggregatedQueryProviderResults = queryProviderTaskResultsList
            .SelectMany(r => r)
            .ToList();

        logger.LogInformation(
            "Aggregated {Count} results before PEP filtering",
            aggregatedQueryProviderResults.Count
        );

        var pepFilterTasks = new List<Task<IReadOnlyList<SearchResultWithDecision>>>();

        foreach (var provider in availableProviders)
        {
            var providerResults = aggregatedQueryProviderResults
                .Where(searchResultItem =>
                    string.Equals(
                        searchResultItem.CustodianId,
                        provider.OrgId,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && string.Equals(
                        searchResultItem.RecordType,
                        provider.RecordType,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            if (providerResults.Count == 0)
                continue;

            var filterInput = new FilterResultsInput(
                provider.OrgId,
                data.PolicyContext.ClientId,
                data.PolicyContext.OrgType,
                providerResults,
                provider.DsaPolicy,
                data.PolicyContext.Purpose
            );

            pepFilterTasks.Add(
                context.CallActivityAsync<IReadOnlyList<SearchResultWithDecision>>(
                    "FilterResultsByPolicyFunction",
                    filterInput,
                    options
                )
            );
        }

        var pepResultsTaskList = await Task.WhenAll(pepFilterTasks);
        var pepResults = pepResultsTaskList.SelectMany(r => r).ToList();

        logger.LogInformation(
            "Filtered to {Count} results after PEP enforcement",
            pepResults.Count(x => x.Decision.IsAllowed)
        );

        // Audit PEP decisions
        await context.CallActivityAsync(
            nameof(AuditPepFindActivity),
            new AuditPepFindInput(data.PolicyContext, data.Metadata, pepResults),
            options
        );

        return pepResults.Where(x => x.Decision.IsAllowed).Select(x => x.Item).ToList();
    }
}
