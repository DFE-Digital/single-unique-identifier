namespace SUI.Find.Application.Models;

public record SearchWorkItemDto
{
    public string WorkItemId { get; init; } = null!;
    public string PersonId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
