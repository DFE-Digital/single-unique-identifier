using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IFindApiClient
{
    Task<JobInfo?> ClaimAsync(string token);
    Task SubmitAsync(string token, SubmitJobResultsRequest request);
}
