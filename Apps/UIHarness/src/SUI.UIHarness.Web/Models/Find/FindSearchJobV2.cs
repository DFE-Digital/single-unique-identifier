using System.Text.Json.Serialization;

namespace SUI.UIHarness.Web.Models.Find;

public record FindSearchJobV2
{
    public required string WorkItemId { get; init; }
    public string Suid { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = [];
}
