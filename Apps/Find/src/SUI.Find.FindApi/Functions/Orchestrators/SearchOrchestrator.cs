using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.Orchestrators;

[ExcludeFromCodeCoverage(
    Justification = "Not Implemented - to be completed as part of future work."
)]
public class SearchOrchestrator
{
    [Function("SearchOrchestrator")]
    public static async Task<SearchResultItem[]> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context
    )
    {
        var data = context.GetInput<SearchOrchestratorInput>();
        // TODO: Activities
        return [];
    }
}
