namespace SUI.Find.Application.Models;

public record JobResultMessage
{
    public required string JobId { get; init; }

    public required string JobType { get; init; }

    public string? WorkItemType { get; init; }

    public required string WorkItemId { get; init; }

    public required string LeaseId { get; init; }

    public required string CustodianId { get; init; }

    public required DateTimeOffset SubmittedAtUtc { get; init; }

    public required List<JobResultRecord> Records { get; init; }

    //public string? TraceParent { get; init; }
}
