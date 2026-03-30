using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Models;

public record SearchResultsV2Dto : SearchResultsDto
{
    public string WorkItemId { get; init; } = string.Empty;
    public int CompletenessPercentage { get; init; }
}
