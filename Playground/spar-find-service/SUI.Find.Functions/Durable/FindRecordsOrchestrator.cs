using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using Microsoft.DurableTask;
using Models;
using Services;

public sealed class FindRecordsOrchestrator
{
    [Function("FindRecordsOrchestrator")]
    public async Task<IReadOnlyList<SearchResultItem>> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<FindOrchestrationInput>();

        var jobId = context.InstanceId;
        var sui = input.Sui;
        var metadata = input.Metadata;
        var policy = input.Policy;

        context.SetCustomStatus(new
        {
            state = "running",
            requestedAtUtc = metadata.RequestedAtUtc
        });

        var providers = await context.CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
            "GetProvidersActivity",
            null);

        var tasks = new List<Task<IReadOnlyList<SearchResultItem>>>(providers.Count);

        foreach (var provider in providers)
        {
            tasks.Add(context.CallActivityAsync<IReadOnlyList<SearchResultItem>>(
                "QueryProviderActivity",
                new QueryProviderInput(jobId, sui, provider)));
        }

        var results = await Task.WhenAll(tasks);

        var items = results
            .SelectMany(r => r)
            .ToList();

        context.SetCustomStatus(new
        {
            state = "completed"
        });

        return items;
    }
}
