using System.Text.Json.Serialization;

namespace SUI.UIHarness.Web.Models.Find;

public record FindSearchResults()
{
    public required string JobId { get; set; } = string.Empty;
    public required string Suid { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required FindSearchStatus Status { get; set; }
    public required FindSearchResultItem[] Items { get; set; } = [];

    [JsonPropertyName("_links")]
    public required Dictionary<string, HalLink> Links { get; set; } = [];
}
