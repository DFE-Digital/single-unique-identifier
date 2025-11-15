namespace SUI.Find.FindApi.Models;

public record SearchResults
{
  public required string JobId { get; set; } = string.Empty;
  public required string Suid { get; set; } = string.Empty;
  public required SearchStatus Status { get; set; }
  public required List<RecordEndpoint> Items { get; set; } = [];
  public required Dictionary<string, HalLink> Links { get; set; } = [];

}
