namespace SUI.StubCustodians.Application.Models;

public sealed record SearchResultItem(
    string ProviderSystem,
    string ProviderName,
    string RecordType,
    string RecordUrl
);
