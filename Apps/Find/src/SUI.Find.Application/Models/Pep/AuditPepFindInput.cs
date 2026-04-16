namespace SUI.Find.Application.Models.Pep;

public record AuditPepFindInput(
    PolicyContext PolicyContext,
    SearchJobMetadata Metadata,
    List<PepResultItem<CustodianSearchResultItem>> SearchResultsWithDecisions
);
