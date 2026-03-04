namespace SUI.Find.Infrastructure.Repositories.JobRepository;

public class Job
{
    public required string JobId { get; init; }
    public required string CustodianId { get; init; }
    public required JobType JobType { get; init; }
    public WorkItemType? WorkItemType { get; init; }
    public string? WorkItemId { get; init; }
    public string? LeaseId { get; init; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; init; }
    public int AttemptCount { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public required string PayloadJson { get; init; }
    public string? JobTraceParent { get; init; }
    public string? ETag { get; init; }
}

public enum WorkItemType
{
    Unknown,

    /// <summary>
    /// This Work Item deals with responding to a Search Request - a request to find records for a given SUI.
    /// </summary>
    SearchExecution,
}

public enum JobType
{
    Unknown,

    /// <summary>
    /// Custodian Lookup jobs indicate the Custodian needs to look for the records they hold about a specific SUI,
    /// and then submit pointers to those records back to the SUI System.
    /// Custodian Lookup is essentially the job in response to a Search Request.
    /// </summary>
    CustodianLookup,
}
