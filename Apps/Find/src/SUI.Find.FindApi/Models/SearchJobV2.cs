using System.Text.Json.Serialization;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchJobV2
{
    public required string WorkItemId { get; init; }
    public string Suid { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links
    {
        get
        {
            var links = new Dictionary<string, HalLink>
            {
                { "results", new HalLink($"/v2/searches/{WorkItemId}/results", "GET") },
            };

            return links;
        }
    }

    public static SearchJobV2 FromDto(SearchWorkItemDto dto)
    {
        return new SearchJobV2
        {
            WorkItemId = dto.WorkItemId,
            Suid = dto.PersonId,
            CreatedAt = dto.CreatedAt,
        };
    }
}
