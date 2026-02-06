namespace SUI.Find.Application.Models.AuditPayloads;

public record PepFindRecordDetail
{
    // What record and how it mapped
    public required string SourceOrgId { get; init; } // Who owned the record
    public required string RecordUrl { get; init; } // The un-masked URL of the record
    public required string RecordType { get; init; }

    // Policy decision snapshot
    public required bool IsSharedAllowed { get; init; }
    public required string RuleType { get; init; } // "exception" or "default"
    public required string RuleEffect { get; init; } // "allow" or "deny"
    public DateTimeOffset? RuleValidFrom { get; init; }
    public DateTimeOffset? RuleValidUntil { get; init; }
    public required string DecisionReason { get; init; } // e.g., "Matched default rule: effect=allow, recordType=children_social_care"
}
