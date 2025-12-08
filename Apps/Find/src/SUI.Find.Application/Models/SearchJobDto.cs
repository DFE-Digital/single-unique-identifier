using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public record SearchJobDto
{
    public string JobId { get; init; } = null!;
    public string PersonId { get; init; } = string.Empty;
    public SearchStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
}
