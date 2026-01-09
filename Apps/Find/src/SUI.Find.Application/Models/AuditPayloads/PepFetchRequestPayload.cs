using System.Text.Json.Serialization;
using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models.AuditPayloads;

// Questions used to determine what we need to capture:
// If an organisation says they were missing some information when making a decision after fetching a record, how do we prove that they did or did not receive it? and the correct record was fetched?
// If an auditor wants to see what information was fetched by whom and when. How can we show them with all the necessary details to be able to know exactly who got what, when and why?

/// <summary>
/// Represents the PEP-specific audit details for a fetch record attempt.
/// Combined with AuditEvent metadata (Timestamp, Actor, CorrelationId) provides complete forensic evidence.
/// Its purpose is to answer Who, What, Why, and Outcome for any given PEP-protected fetch request.
/// </summary>
public record PepFetchPayload
{
    public required string DestinationOrgId { get; init; } // Who requested the records
    public required string Purpose { get; init; } // Why the records were requested

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required FetchOutcome FetchOutcome { get; init; }
    public required PepFindRecordDetail? Record { get; init; }
}
