namespace SUI.Find.FindApi.Models;

public record SearchResultItem
{
  public required string ProviderSystem { get; set; }
  public required string ProviderName { get; set; }
  public required string RecordType { get; set; }
  public required string RecordUrl { get; set; }
}

