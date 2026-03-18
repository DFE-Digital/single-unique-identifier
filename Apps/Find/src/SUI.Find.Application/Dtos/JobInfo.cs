namespace SUI.Find.Application.Dtos;

public record JobInfo
{
    public required string JobId { get; init; }
    public required string CustodianId { get; init; }
    public required DateTimeOffset LeaseExpiresAtUtc { get; init; }
    public required string LeaseId { get; init; }
    public string? WorkItemId { get; init; }
    public string? Sui { get; init; }
    public string? RecordType { get; init; }
}
