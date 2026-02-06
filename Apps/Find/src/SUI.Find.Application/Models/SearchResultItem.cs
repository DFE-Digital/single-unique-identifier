namespace SUI.Find.Application.Models;

public sealed record SearchResultItem(
    string RecordType,
    string RecordUrl,
    string? SystemId,
    string? RecordId
);
