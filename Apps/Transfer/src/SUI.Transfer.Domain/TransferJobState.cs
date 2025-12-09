using System.Text.Json.Serialization;
using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Domain;

public abstract record TransferJobState(
    [property: JsonPropertyOrder(0)] Guid JobId,
    [property: JsonPropertyOrder(1)] string Sui,
    [property: JsonPropertyOrder(2)] TransferJobStatus Status,
    [property: JsonPropertyOrder(3)] DateTimeOffset CreatedAt,
    [property: JsonPropertyOrder(4)] DateTimeOffset LastUpdatedAt,
    [property: JsonPropertyName("data")]
    [property: JsonPropertyOrder(5)]
        ConformedData? ConformedData = null
)
{
    [JsonPropertyOrder(6)]
    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links
    {
        get
        {
            var links = new Dictionary<string, HalLink>
            {
                { "status", new HalLink($"/v1/transfer/{JobId}", "GET") },
                { "results", new HalLink($"/v1/transfer/{JobId}/results", "GET") },
                { "cancel", new HalLink($"/v1/transfer/{JobId}", "DELETE") },
            };

            return links;
        }
    }
}

public record CompletedTransferJobState(
    Guid JobId,
    string Sui,
    ConformedData ConformedData,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt
)
    : TransferJobState(
        JobId,
        Sui,
        TransferJobStatus.Completed,
        CreatedAt,
        LastUpdatedAt,
        ConformedData
    );

public record QueuedTransferJobState(Guid JobId, string Sui, DateTimeOffset CreatedAt)
    : TransferJobState(JobId, Sui, TransferJobStatus.Queued, CreatedAt, CreatedAt);

public record RunningTransferJobState(
    Guid JobId,
    string Sui,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt
) : TransferJobState(JobId, Sui, TransferJobStatus.Running, CreatedAt, LastUpdatedAt);

public record FailedTransferJobState(
    Guid JobId,
    string Sui,
    string ErrorMessage,
    string StackTrace,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt
) : TransferJobState(JobId, Sui, TransferJobStatus.Failed, CreatedAt, LastUpdatedAt);

public record CancelledTransferJobState(
    Guid JobId,
    string Sui,
    string CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt
) : TransferJobState(JobId, Sui, TransferJobStatus.Canceled, CreatedAt, LastUpdatedAt);
