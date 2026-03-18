using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public record SearchResultsV2Dto
{
    public string WorkItemId { get; init; } = string.Empty;
    public string Suid { get; init; } = string.Empty;
    public SearchStatus Status { get; init; }
    public IReadOnlyList<SearchResultEntry> Items { get; init; } = [];
    public int CompletenessPercentage { get; set; }
}
