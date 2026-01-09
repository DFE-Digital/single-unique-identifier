namespace SUI.Find.Application.Models;

public sealed record SearchResultItem(
    string ProviderSystem,
    string ProviderId,
    string RecordType,
    string RecordUrl
);
