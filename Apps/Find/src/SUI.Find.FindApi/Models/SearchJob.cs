using System.Text.Json.Serialization;

namespace SUI.Find.FindApi.Models;

public record SearchJob
{
  public required string JobId { get; init; }
  public string Suid { get; init; } = string.Empty;
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public SearchStatus Status { get; init; }
  public DateTime CreatedAt { get; init; }
  public DateTime LastUpdatedAt { get; init; }

  [JsonPropertyName("_links")]
  public Dictionary<string, HalLink> Links { get; init; } = [];
}
