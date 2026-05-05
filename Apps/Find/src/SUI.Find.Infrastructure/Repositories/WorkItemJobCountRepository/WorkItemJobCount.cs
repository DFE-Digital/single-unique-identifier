using System.Collections.Frozen;
using SUI.Find.Application.Enums;

namespace SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

public class WorkItemJobCount
{
    public required string WorkItemId { get; init; }
    public required JobType JobType { get; init; }
    public int ExpectedJobCount { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public string? RequestingOrganisationId { get; init; }
    public required string PayloadJson { get; init; }
    public FrozenSet<string> CompletedJobIds { get; init; } = [];
}
