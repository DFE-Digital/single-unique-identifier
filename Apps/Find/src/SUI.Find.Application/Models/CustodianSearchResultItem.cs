namespace SUI.Find.Application.Models;

public sealed record CustodianSearchResultItem(
    string CustodianId,
    string RecordType,
    string RecordUrl,
    string? SystemId,
    string? RecordId
)
{
    public static CustodianSearchResultItem Create(
        string custodianId,
        SearchResultItem searchResultItem
    ) =>
        new(
            custodianId,
            searchResultItem.RecordType,
            searchResultItem.RecordUrl,
            searchResultItem.SystemId,
            searchResultItem.RecordId
        );
}
