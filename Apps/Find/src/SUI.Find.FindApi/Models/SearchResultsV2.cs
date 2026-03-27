using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchResultsV2 : SearchResultsBase
{
    public required string WorkItemId { get; set; } = string.Empty;

    public required int CompletenessPercentage { get; set; }

    public required SearchResultEntry[] Items { get; set; } = [];

    public static SearchResultsV2 FromDto(SearchResultsV2Dto dto)
    {
        return new SearchResultsV2
        {
            WorkItemId = dto.WorkItemId,
            Suid = dto.Suid,
            Status = dto.Status,
            CompletenessPercentage = dto.CompletenessPercentage,
            Items = dto.Items,
        };
    }
}
