namespace SUI.Find.FindApi.Models;

public class SearchJob
{
  public string JobId { get; set; } = string.Empty;
  public string Suid { get; set; } = string.Empty;
  public SearchStatus Status { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime LastUpdatedAt { get; set; }
  public Dictionary<string, HalLink> Links { get; set; } = [];

  public SearchJob()
  {
    Links = new Dictionary<string, HalLink>
    {
      ["_links"] = new HalLink()
    };
  }
}
