namespace SUI.Find.Application.Models;

public sealed record SearchResultItem
{
    private const string DefaultSystem = "DefaultSystem";

    public required string RecordType { get; init; }
    public required string RecordUrl { get; init; }

    public string SystemId
    {
        get;
        init => field = string.IsNullOrWhiteSpace(value) ? DefaultSystem : value;
    } = DefaultSystem;

    public string? RecordId { get; init; }
}
