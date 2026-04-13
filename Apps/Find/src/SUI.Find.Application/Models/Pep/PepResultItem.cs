namespace SUI.Find.Application.Models.Pep;

public record PepResultItem<TItem>(
    TItem Item,
    string SourceOrgId,
    string DestOrgId,
    PolicyDecisionResult Decision
)
    where TItem : IPepFilterable;
