using System.Text.Json.Serialization;

namespace SUI.UIHarness.Web.Models.Find;

public record FindSearchJob
{
    public required string JobId { get; init; }
    public string Suid { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FindSearchStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; }
}
