namespace SUI.Find.FindApi.Models;

public class SearchResults
{
  public string JobId { get; set; } = string.Empty;
  public string Suid { get; set; } = string.Empty;
  public SearchStatus Status { get; set; }
  public List<RecordEndpoint> Items { get; set; } = [];
  public Dictionary<string, HalLink> Links { get; set; } = [];

  public SearchResults()
  {
    Links = new Dictionary<string, HalLink>
    {
      ["_links"] = new HalLink()
    };
  }
}
