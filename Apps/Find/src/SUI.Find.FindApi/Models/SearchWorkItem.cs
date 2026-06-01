using System.Text.Json.Serialization;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchWorkItem
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

    public static SearchWorkItem FromDto(SearchWorkItemDto dto)
    {
        return new SearchWorkItem
        {
            WorkItemId = dto.WorkItemId,
            Suid = dto.PersonId,
            CreatedAt = dto.CreatedAt,
        };
    }
}
