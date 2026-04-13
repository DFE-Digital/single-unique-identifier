using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public record JobResultMessage
{
    public required string JobId { get; init; }

    public required JobType JobType { get; init; }

    public WorkItemType? WorkItemType { get; init; }

    public required string WorkItemId { get; init; }

    public required string LeaseId { get; init; }

    public required string CustodianId { get; init; }

    public required DateTimeOffset SubmittedAtUtc { get; init; }

    public required List<JobResultRecord> Records { get; init; }

    public string? JobTraceParent { get; init; }
}
