using System.Text.Json.Serialization;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchResults : SearchResultsBase
{
    public required string JobId { get; set; } = string.Empty;

    [JsonPropertyName("_links")]
    public required Dictionary<string, HalLink> Links { get; set; } = [];

    public required SearchResultItem[] Items { get; set; } = [];

    public static SearchResults FromDto(SearchResultsDto dto)
    {
        return new SearchResults
        {
            JobId = dto.JobId,
            Suid = dto.Suid,
            Status = dto.Status,
            Items = dto.Items,
            Links = new Dictionary<string, HalLink>
            {
                { "self", new HalLink($"/search/{dto.JobId}/results", "GET") },
                { "job", new HalLink($"/search/{dto.JobId}", "GET") },
            },
        };
    }
}
