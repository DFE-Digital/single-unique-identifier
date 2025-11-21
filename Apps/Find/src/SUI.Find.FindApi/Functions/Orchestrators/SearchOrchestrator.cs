using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace SUI.Find.FindApi.Functions.Orchestrators;

[ExcludeFromCodeCoverage(Justification = "Not implemented yet")]
public class SearchOrchestrator
{
    [Function("SearchOrchestrator")]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context
    )
    {
        var suid = context.GetInput<string>();
        // TODO: Activities
        return await Task.FromResult($"Search completed for SUID: {suid}");
    }
}
