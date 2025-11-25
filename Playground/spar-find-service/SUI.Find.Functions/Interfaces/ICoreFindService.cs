using Grpc.Core;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using Microsoft.DurableTask.Client;
using Models;
using static Models.PersonMatch;

namespace Interfaces;

public interface ICoreFindService
{
    Task StartSearchAsync(DurableTaskClient client, string jobId, string sui, string personId, PolicyContext policy, CancellationToken ct);
    Task<IReadOnlyList<SearchResultItem>> GetRawResultsAsync(DurableTaskClient client, string jobId, CancellationToken ct);
    Task<(SearchStatus Status, DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt, SearchMetadata Metadata)> GetRuntimeStatusAsync(DurableTaskClient client, string jobId, CancellationToken ct);
    Task CancelAsync(DurableTaskClient client, string jobId, CancellationToken ct);
}
