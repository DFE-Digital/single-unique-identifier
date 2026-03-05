using System.Text.Json.Serialization;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Models;

public record SearchJob
{
    public required string JobId { get; init; }
    public string Suid { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SearchStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links
    {
        get
        {
            var links = new Dictionary<string, HalLink>
            {
                { "self", new HalLink($"/v1/searches/{JobId}", "GET") },
                { "status", new HalLink($"/v1/searches/{JobId}", "GET") },
                { "results", new HalLink($"/v1/searches/{JobId}/results", "GET") },
                { "cancel", new HalLink($"/v1/searches/{JobId}", "DELETE") },
            };

            return links;
        }
    }

    public static SearchJob FromDto(SearchJobDto dto)
    {
        return new SearchJob
        {
            JobId = dto.JobId,
            Suid = dto.PersonId,
            Status = dto.Status,
            CreatedAt = dto.CreatedAt,
            LastUpdatedAt = dto.LastUpdatedAt,
        };
    }
}
