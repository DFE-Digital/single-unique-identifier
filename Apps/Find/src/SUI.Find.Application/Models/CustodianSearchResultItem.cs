namespace SUI.Find.Application.Models;

public sealed record CustodianSearchResultItem(
    string? SystemId,
    string? RecordId,
    string RecordType,
    string RecordUrl,
    string CustodianId
)
{
    public static CustodianSearchResultItem Create(
        string custodianId,
        SearchResultItem searchResultItem
    ) =>
        new(
            searchResultItem.SystemId,
            searchResultItem.RecordId,
            searchResultItem.RecordType,
            searchResultItem.RecordUrl,
            custodianId
        );
}
