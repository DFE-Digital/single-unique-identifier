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

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required RequestStatus RequestStatus { get; init; }
    public required string RequestStatusMessage { get; init; }
    public required string Purpose { get; init; } // Why the records were requested

    [JsonConverter(typeof(JsonStringEnumConverter))]
    // Keeping a top level for easy querying of policy outcome
    public PolicyDecision PolicyOutcome => DeterminePolicyOutcome();
    public required PepFindRecordDetail? PolicySnapshot { get; init; }

    public required DateTimeOffset RequestStartedAt { get; init; }
    public required DateTimeOffset RequestFinishedAt { get; init; }
    public required int ReceivedByteCount { get; init; } // Size of data received in response from third party

    private PolicyDecision DeterminePolicyOutcome()
    {
        if (PolicySnapshot is null)
        {
            return PolicyDecision.Indeterminate;
        }

        return PolicySnapshot.IsSharedAllowed ? PolicyDecision.Allowed : PolicyDecision.Denied;
    }
}
