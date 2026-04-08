namespace SUI.Find.Application.Models;

public record RenewJobLeaseResponse
{
    public required string JobId { get; init; }

    public required string WorkItemId { get; init; }

    public required string LeaseId { get; init; }

    public required DateTimeOffset LeaseExpiresUtc { get; init; }
}
