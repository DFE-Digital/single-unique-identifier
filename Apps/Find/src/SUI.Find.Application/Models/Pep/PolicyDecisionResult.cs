namespace SUI.Find.Application.Models.Pep;

public record PolicyDecisionResult
{
    public bool IsAllowed { get; init; }
    public required string Reason { get; init; }

    // ADD these for audit trail
    public string? RuleType { get; init; } // "exception" or "default"
    public string? RuleEffect { get; init; } // "allow" or "deny"
    public DateTimeOffset? ValidFrom { get; init; }
    public DateTimeOffset? ValidUntil { get; init; }
}
