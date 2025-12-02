using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public record SearchResultsDto
{
    public string JobId { get; init; } = string.Empty;
    public string Suid { get; init; } = string.Empty;
    public SearchStatus Status { get; init; }
    public SearchResultItem[] Items { get; init; } = [];
}
