namespace SUI.Find.Domain.Models.Policy;

public record PolicyDecision(
    bool IsAllowed,
    string Reason,
    string PolicyVersionId
);