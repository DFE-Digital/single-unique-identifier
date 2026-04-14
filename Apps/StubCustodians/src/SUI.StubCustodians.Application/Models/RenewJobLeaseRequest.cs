namespace SUI.StubCustodians.Application.Models;

public record RenewJobLeaseRequest
{
    public required string JobId { get; init; }

    public required string LeaseId { get; init; }
}
