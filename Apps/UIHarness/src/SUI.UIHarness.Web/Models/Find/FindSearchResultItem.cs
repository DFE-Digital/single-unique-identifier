namespace SUI.UIHarness.Web.Models.Find;

public sealed record FindSearchResultItem(
    string RecordType,
    string RecordUrl,
    string? SystemId,
    string? SystemName,
    string? RecordId
);
