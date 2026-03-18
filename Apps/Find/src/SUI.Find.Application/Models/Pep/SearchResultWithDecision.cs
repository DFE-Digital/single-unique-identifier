namespace SUI.Find.Application.Models.Pep;

public record SearchResultWithDecision(
    CustodianSearchResultItem Item,
    string SourceOrgId,
    string DestOrgId,
    PolicyDecisionResult Decision
);
