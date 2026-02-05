namespace SUI.StubCustodians.Application.Models;

public sealed record SearchResultItem(
    string RecordType,
    string RecordUrl,
    string? RecordId,
    string? SystemId
);
