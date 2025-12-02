using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.Orchestrators;

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
        var options = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 2.0
        ));

        var data = context.GetInput<SearchOrchestratorInput>();

        if (data is null || string.IsNullOrWhiteSpace(data.Suid) || string.IsNullOrWhiteSpace(data.Metadata.PersonId) || string.IsNullOrWhiteSpace(data.PolicyContext.ClientId))
        {
            logger.LogError("Invalid input in Search Orchestrator: {InstanceId}", context.InstanceId);
            throw new Exception("Invalid input in Search Orchestrator");
        }

        logger.LogInformation("Search Orchestrator started for InstanceId: {InstanceId}", context.InstanceId);

        try
        {
            var availableProviders =
                await context.CallActivityAsync<List<ProviderDefinition>>("GetProvidersFunction", data.Suid);

            if (availableProviders.Count == 0)
            {
                logger.LogWarning("No available providers found for InstanceId: {InstanceId}", context.InstanceId);

                return new List<SearchResultItem>();
            }

            var tasks = new List<Task<IReadOnlyList<SearchResultItem>>>(availableProviders.Count);

            foreach (var provider in availableProviders)
            {
                tasks.Add(context.CallActivityAsync<IReadOnlyList<SearchResultItem>>("QueryProvidersFunction",
                    new QueryProviderInput(data.PolicyContext.ClientId, context.InstanceId, data.Suid, provider),
                    options));
            }

            var results = await Task.WhenAll(tasks);

            var aggregatedResults = results.SelectMany(r => r).ToList();

            return aggregatedResults;

        }
        catch (TaskFailedException ec)
        {
            // TODO : Implement proper DLQ handling and logging
            logger.LogError(ec, "Search Orchestrator failed after all retry attempts for InstanceId: {InstanceId}", context.InstanceId);
            throw;
        }

    }
}
