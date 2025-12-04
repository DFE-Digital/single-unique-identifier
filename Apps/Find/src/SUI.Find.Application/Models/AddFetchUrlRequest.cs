namespace SUI.Find.Application.Models;

public record AddFetchUrlRequest
{
    public required string FetchId { get; set; }
    public required string JobId { get; set; }
    public required string TargetUrl { get; set; }
    public required string TargetOrg { get; set; }
    public required string RequestingOrg { get; set; }
    public required string RecordType { get; set; }
    public required TimeSpan Ttl { get; set; }
}
