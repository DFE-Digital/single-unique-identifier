using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IFindApiClient
{
    string? BaseAddress { get; }
    Task<JobInfo?> ClaimAsync(string token);
    Task<RenewJobLeaseResponse?> ExtendLeaseAsync(string token, RenewJobLeaseRequest request);
    Task SubmitAsync(string token, SubmitJobResultsRequest request);
}
