using Microsoft.DurableTask;

namespace SUI.Find.FindApi;

[DurableTask("SearchOrchestrator")]
public class SearchDurableTasks : TaskOrchestrator<string, string>
{
    public override async Task<string> RunAsync(TaskOrchestrationContext context, string input)
    {
        return await context.CallActivityAsync<string>(input);
    }
}
