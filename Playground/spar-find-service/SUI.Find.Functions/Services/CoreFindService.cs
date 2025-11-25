using Interfaces;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Models;
using static Models.PersonMatch;

namespace Services;

public sealed record FindOrchestrationInput(string Sui, SearchMetadata Metadata, PolicyContext Policy);

public sealed class CoreFindService : ICoreFindService
{
    public async Task StartSearchAsync(
        DurableTaskClient client,
        string jobId,
        string sui,
        string personId,
        PolicyContext policy,
        CancellationToken ct)
    {
        var metadata = new SearchMetadata(
            PersonId: personId,
            RequestedAtUtc: DateTimeOffset.UtcNow
        );

        var input = new FindOrchestrationInput(
            Sui: sui,
            Metadata: metadata,
            Policy: policy
        );

        await client.ScheduleNewOrchestrationInstanceAsync(
            orchestratorName: "FindRecordsOrchestrator",
            input: input,
            options: new StartOrchestrationOptions { InstanceId = jobId },
            cancellation: ct);
    }

    public async Task<IReadOnlyList<SearchResultItem>> GetRawResultsAsync(
        DurableTaskClient client,
        string jobId,
        CancellationToken ct)
    {
        var meta = await client.GetInstancesAsync(jobId, getInputsAndOutputs: true, cancellation: ct);

        if (meta is null)
        {
            throw new KeyNotFoundException("Job not found.");
        }

        var items = meta.ReadOutputAs<List<SearchResultItem>>();

        if (items is null || items.Count == 0)
        {
            return Array.Empty<SearchResultItem>();
        }

        return items;
    }

    public async Task<(SearchStatus Status, DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt, SearchMetadata Metadata)> GetRuntimeStatusAsync(
        DurableTaskClient client,
        string jobId,
        CancellationToken ct)
    {
        var meta = await client.GetInstanceAsync(
            jobId,
            getInputsAndOutputs: true,
            cancellation: ct);

        if (meta is null)
        {
            throw new KeyNotFoundException("Job not found.");
        }

        var runtime = meta.RuntimeStatus;

        var status =
            runtime == OrchestrationRuntimeStatus.Running ? SearchStatus.Running :
            runtime == OrchestrationRuntimeStatus.Completed ? SearchStatus.Completed :
            runtime == OrchestrationRuntimeStatus.Failed ? SearchStatus.Failed :
            runtime == OrchestrationRuntimeStatus.Terminated ? SearchStatus.Cancelled :
            SearchStatus.Queued;

        SearchMetadata metadata = new(
            PersonId: string.Empty,
            RequestedAtUtc: meta.CreatedAt
        );

        if (!string.IsNullOrWhiteSpace(meta.SerializedInput))
        {
            try
            {
                var input = System.Text.Json.JsonSerializer.Deserialize<FindOrchestrationInput>(
                    meta.SerializedInput,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (input?.Metadata is not null)
                {
                    metadata = input.Metadata;
                }
            }
            catch
            {
            }
        }

        return (status, meta.CreatedAt, meta.LastUpdatedAt, metadata);
    }

    public async Task CancelAsync(DurableTaskClient client, string jobId, CancellationToken ct)
    {
        await client.TerminateInstanceAsync(jobId, "cancelled", cancellation: ct);
    }
}
