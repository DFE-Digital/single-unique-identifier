namespace SUI.Find.FindApi.Models;

public class Problem
{
  public string Type { get; set; } = string.Empty;
  public string Title { get; set; } = string.Empty;
  public int Status { get; set; }
  public string? Detail { get; set; }
  public string? Instance { get; set; }
}
