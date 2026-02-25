namespace SUI.Find.Application.Dtos;

public class SearchResultEntry
{
    public required string CustodianId { get; init; }
    public required string SystemId { get; init; }
    public required string RecordType { get; init; }
    public required string RecordUrl { get; init; }
    public string? RecordId { get; init; }
    public DateTimeOffset SubmittedAtUtc { get; init; }
    public required string JobId { get; init; }
    public required string WorkItemId { get; init; }
}
