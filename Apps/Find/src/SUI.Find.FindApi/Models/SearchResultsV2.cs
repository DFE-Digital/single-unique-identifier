using System.Text.Json.Serialization;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchResultsV2
{
    public required string WorkItemId { get; set; } = string.Empty;
    public required string Sui { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SearchStatus Status { get; set; }

    public required double CompletenessPercentage { get; set; }
    public required IReadOnlyList<SearchResultEntry> Items { get; set; } = [];

    public static SearchResultsV2 FromDto(SearchResultsV2Dto dto)
    {
        return new SearchResultsV2
        {
            WorkItemId = dto.WorkItemId,
            Sui = dto.Suid,
            Status = dto.Status,
            CompletenessPercentage = dto.CompletenessPercentage,
            Items = dto.Items,
        };
    }
}
