using SUI.Find.Application.Enums;

namespace SUI.Find.Infrastructure.Repositories.JobRepository;

public record Job
{
    public required string JobId { get; init; }
    public required string CustodianId { get; init; }
    public required string SearchingOrganisationId { get; init; }
    public required JobType JobType { get; init; }
    public WorkItemType? WorkItemType { get; init; }
    public string? WorkItemId { get; init; }
    public string? LeaseId { get; init; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public int AttemptCount { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public required string PayloadJson { get; init; }
    public string? JobTraceParent { get; init; }
    public string ETag { get; init; } = "*";
}
