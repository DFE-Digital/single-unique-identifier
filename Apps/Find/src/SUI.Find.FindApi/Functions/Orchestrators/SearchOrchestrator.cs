using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace SUI.Find.FindApi.Functions.Orchestrators;

[ExcludeFromCodeCoverage(Justification = "Not implemented yet")]
public class SearchOrchestrator(ILogger<SearchOrchestrator> logger)
{
    [Function("SearchOrchestrator")]
    public async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context
    )
    {
        var model = context.GetInput<SearchOrchestratorParameters>();
        if (model is null)
        {
            throw new InvalidOperationException("Orchestrator input model is null");
        }

        using (
            logger.BeginScope(
                new Dictionary<string, object> { ["CorrelationId"] = model.CorrelationId }
            )
        )
        {
            logger.LogInformation("SearchOrchestrator started");
            return await Task.FromResult("Search completed");
        }
    }
}

public record SearchOrchestratorParameters(string Suid, string CorrelationId);
