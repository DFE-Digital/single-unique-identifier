namespace SUI.Find.Application.Dtos;

public class SearchResultEntry
{
    /// <summary>
    /// Submitting custodian's Org ID
    /// </summary>
    public required string CustodianId { get; init; }

    public required string SystemId { get; init; }

    /// <summary>
    /// Submitting custodian's Org Name
    /// </summary>
    public required string CustodianName { get; init; }

    public required string RecordType { get; init; }
    public required string RecordUrl { get; init; }
    public string? RecordId { get; init; }
    public DateTimeOffset SubmittedAtUtc { get; init; }
    public required string JobId { get; init; }
    public required string WorkItemId { get; init; }
}
