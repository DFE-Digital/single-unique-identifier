using Functions;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using Microsoft.DurableTask.Client;
using Models;

namespace Interfaces;
public interface IFindOrchestrationService
{
    Task<PersonMatch> MatchPersonAsync(AuthContext caller, FindPersonRequest request, CancellationToken ct);
    Task<SearchJob> StartSearchAsync(DurableTaskClient durableClient, AuthContext caller, string personId, CancellationToken ct);
    Task<SearchJob> GetStatusAsync(DurableTaskClient durableClient, AuthContext caller, string jobId, CancellationToken ct);
    Task<SearchResults> GetResultsAsync(DurableTaskClient durableClient, AuthContext caller, string jobId, CancellationToken ct);
    Task<SearchJob> CancelAsync(DurableTaskClient durableClient, AuthContext caller, string jobId, CancellationToken ct);
    Task<string> FetchRecordAsync(AuthContext caller, string fetchId, CancellationToken ct);
}
