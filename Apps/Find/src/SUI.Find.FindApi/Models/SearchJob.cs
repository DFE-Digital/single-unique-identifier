namespace SUI.Find.FindApi.Models;

public record SearchJob
{
  public required string JobId { get; set; }
  public string Suid { get; set; } = string.Empty;
  public SearchStatus Status { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime LastUpdatedAt { get; set; }
  public Dictionary<string, HalLink> Links { get; set; } = [];
}
