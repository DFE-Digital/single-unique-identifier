namespace SUI.Find.Application.Models;

public sealed record SearchResultItem(
    string? SystemId,
    string? RecordId,
    string RecordType,
    string RecordUrl
);
