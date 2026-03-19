using SUI.Find.Infrastructure.Enums;

namespace SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

public class WorkItemJobCount
{
    public required string WorkItemId { get; init; }
    public required JobType JobType { get; init; }
    public int ExpectedJobCount { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public required string SearchingOrganisationId { get; init; }
    public required string PayloadJson { get; init; }
}
