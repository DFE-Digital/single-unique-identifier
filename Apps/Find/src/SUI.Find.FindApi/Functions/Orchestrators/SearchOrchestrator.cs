using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace SUI.Find.FindApi.Functions.Orchestrators;

[ExcludeFromCodeCoverage(
    Justification = "Not Implemented - to be completed as part of future work."
)]
public class SearchOrchestrator
{
    [Function("SearchOrchestrator")]
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context
    )
    {
        var suid = context.GetInput<string>();
        // TODO: Activities
        return $"Search completed for SUID: {suid}";
    }
}
