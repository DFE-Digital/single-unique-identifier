using System.Text.Json.Serialization;
using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Domain;

public abstract record TransferJobState(
    Guid JobId,
    string Sui,
    TransferJobStatus Status,
    DateTimeOffset CreatedAt
)
{
    public DateTimeOffset LastUpdatedAt { get; init; } = TimeProvider.System.GetUtcNow();

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links
    {
        get
        {
            var links = new Dictionary<string, HalLink>
            {
                { "self", new HalLink($"/v1/searches/{JobId}", "GET") },
                { "status", new HalLink($"/v1/searches/{JobId}", "GET") },
                { "cancel", new HalLink($"/v1/searches/{JobId}", "DELETE") },
            };

            if (Status == TransferJobStatus.Completed)
            {
                links.Add("results", new HalLink($"/v1/searches/{JobId}/results", "GET"));
            }

            return links;
        }
    }
}

public record QueuedTransferJobState(Guid JobId, string Sui, DateTimeOffset CreatedAt)
    : TransferJobState(JobId, Sui, TransferJobStatus.Queued, CreatedAt);

public record RunningTransferJobState(Guid JobId, string Sui, DateTimeOffset CreatedAt)
    : TransferJobState(JobId, Sui, TransferJobStatus.Running, CreatedAt);

public record CompletedTransferJobState(
    Guid JobId,
    string Sui,
    ConformedData ConformedData,
    DateTimeOffset CreatedAt
) : TransferJobState(JobId, Sui, TransferJobStatus.Completed, CreatedAt);

public record FailedTransferJobState(
    Guid JobId,
    string Sui,
    string ErrorMessage,
    string? StackTrace,
    DateTimeOffset CreatedAt
) : TransferJobState(JobId, Sui, TransferJobStatus.Failed, CreatedAt);

public record CancelledTransferJobState(
    Guid JobId,
    string Sui,
    string CancellationReason,
    DateTimeOffset CreatedAt
) : TransferJobState(JobId, Sui, TransferJobStatus.Canceled, CreatedAt);
