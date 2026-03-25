namespace SUI.Find.Application.Models;

public sealed record CustodianSearchResultItem(
    string CustodianId,
    string RecordType,
    string RecordUrl,
    string SystemId,
    string CustodianName,
    string? RecordId
) : IPepFilterable
{
    public static CustodianSearchResultItem Create(
        string custodianId,
        string custodianName,
        SearchResultItem searchResultItem
    ) =>
        new(
            custodianId,
            searchResultItem.RecordType,
            searchResultItem.RecordUrl,
            searchResultItem.SystemId,
            custodianName,
            searchResultItem.RecordId
        );
}
