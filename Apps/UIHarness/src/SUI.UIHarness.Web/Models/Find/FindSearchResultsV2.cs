using System.Text.Json.Serialization;

namespace SUI.UIHarness.Web.Models.Find;

public record FindSearchResultsV2
{
    public required string WorkItemId { get; set; } = string.Empty;
    public required string Suid { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required FindSearchStatus Status { get; set; }

    public required int CompletenessPercentage { get; set; }
    public required IReadOnlyList<FindSearchResultItem> Items { get; set; } = [];
}
