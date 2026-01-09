namespace SUI.Find.Application.Models.Pep;

public record SearchResultWithDecision(
    SearchResultItem Item,
    string SourceOrgId,
    PolicyDecisionResult Decision
);
