using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.OrchestratorFunctions;

[ExcludeFromCodeCoverage(
    Justification = "Not Implemented - to be completed as part of future work."
)]
public class SearchOrchestrator(ILogger<SearchOrchestrator> logger)
{
    [Function("SearchOrchestrator")]
    public async Task<List<SearchResultItem>> RunOrchestrator(
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

        using var logScope = logger.BeginScope(
            "CorrelationId: {CorrelationId}",
            data.Metadata.InvocationId
        );

        logger.LogInformation("Search Orchestrator started");

        var availableProviders = await context.CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
            "GetProvidersFunction",
            data.Suid
        );

        if (availableProviders.Count == 0)
        {
            logger.LogWarning("No available providers found");

            return new List<SearchResultItem>();
        }

        var tasks = new List<Task<IReadOnlyList<SearchResultItem>>>(availableProviders.Count);

        foreach (var provider in availableProviders)
        {
            tasks.Add(
                context.CallActivityAsync<IReadOnlyList<SearchResultItem>>(
                    "QueryProvidersFunction",
                    new QueryProviderInput(
                        data.PolicyContext.ClientId,
                        context.InstanceId,
                        data.Metadata.InvocationId,
                        data.Suid,
                        provider
                    ),
                    options
                )
            );
        }

        var taskResultsList = await Task.WhenAll(tasks);

        var aggregatedResults = taskResultsList.SelectMany(r => r).ToList();

        logger.LogInformation(
            "Aggregated {Count} results before PEP filtering",
            aggregatedResults.Count
        );

        var pepFilterTasks = new List<Task<IReadOnlyList<SearchResultItem>>>();

        foreach (var provider in availableProviders)
        {
            var providerResults = aggregatedResults
                .Where(r =>
                    r.ProviderSystem == provider.ProviderSystem
                    && r.RecordType == provider.RecordType
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
                context.CallActivityAsync<IReadOnlyList<SearchResultItem>>(
                    "FilterResultsByPolicyFunction",
                    filterInput,
                    options
                )
            );
        }

        var filteredResultsList = await Task.WhenAll(pepFilterTasks);
        var filteredResults = filteredResultsList.SelectMany(r => r).ToList();

        logger.LogInformation(
            "Filtered to {Count} results after PEP enforcement",
            filteredResults.Count
        );

        return filteredResults;
    }
}
