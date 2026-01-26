namespace SUI.StubCustodians.Application.Models;

public sealed record SearchResultItem(
    string ProviderSystem,
    string ProviderId,
    string RecordType,
    string RecordUrl
);
