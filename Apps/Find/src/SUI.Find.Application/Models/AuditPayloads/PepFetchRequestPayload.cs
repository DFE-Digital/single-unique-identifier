using System.Text.Json.Serialization;
using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models.AuditPayloads;

// Questions used to determine what we need to capture:
// If an organisation sais they were missing some information when making a decision after fetching a record, how do we prove that they did or did not receive it? and the correct record was fetched?
// If an auditor wants to see what information was fetched by whom and when. How can we show them with all the necessary details to be able to know exactly who got what, when and why?

/// <summary>
/// Represents a self-contained audit record for a single 'fetch record' attempt.
/// Its purpose is to answer Who, What, When, Why, and the Outcome for any given request.
/// </summary>
public record PepFetchPayload
{
    public required string DestinationOrgId { get; init; } // Who requested the records
    public required string Purpose { get; init; } // Why the records were requested
    public required DateTimeOffset RequestTimestamp { get; init; } // When the request was made
    public DateTimeOffset? ResponseTimestamp { get; init; } // When the response was received

    [Newtonsoft.Json.JsonConverter(typeof(JsonStringEnumConverter))]
    public required FetchOutcome FetchOutcome { get; init; }
    public required PepFindRecordDetail? Record { get; init; }
}
