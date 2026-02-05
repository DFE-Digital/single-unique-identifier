namespace SUI.Find.Application.Models;

public sealed record CustodianSearchResultItem(
    string RecordType,
    string RecordUrl,
    string CustodianId,
    string? SystemId,
    string? RecordId
)
{
    public static CustodianSearchResultItem Create(
        string custodianId,
        SearchResultItem searchResultItem
    ) =>
        new(
            searchResultItem.RecordType,
            searchResultItem.RecordUrl,
            custodianId,
            searchResultItem.SystemId,
            searchResultItem.RecordId
        );
}
