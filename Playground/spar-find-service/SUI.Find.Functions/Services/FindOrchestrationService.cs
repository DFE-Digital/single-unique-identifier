using Interfaces;
using Microsoft.DurableTask.Client;
using Models;

namespace Services;

public sealed class FindOrchestrationService(
    ICoreFindService core,
    IPepService pep,
    IPersonMatchService personMatch,
    IPersonIdEncryptionService crypto,
    ICallerEncryptionResolver encryptionResolver)
    : IFindOrchestrationService
{
    private readonly ICoreFindService _core = core;
    private readonly IPepService _pep = pep;
    private readonly IPersonMatchService _personMatch = personMatch;
    private readonly IPersonIdEncryptionService _crypto = crypto;
    private readonly ICallerEncryptionResolver _encryptionResolver = encryptionResolver;

    public async Task<PersonMatch> MatchPersonAsync(AuthContext caller, FindPersonRequest request, CancellationToken ct)
    {
        DateOnly? birth = null;

        if (!string.IsNullOrWhiteSpace(request.BirthDate))
        {
            if (!DateOnly.TryParse(request.BirthDate, out var parsed))
            {
                throw new ArgumentException("birthDate must be yyyy-MM-dd");
            }

            birth = parsed;
        }

        var spec = new PersonSpecification
        {
            Given = request.Given,
            Family = request.Family,
            BirthDate = birth,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            AddressPostalCode = request.AddressPostalCode
        };

        var nhs = _personMatch.FindExactNhsNumber(spec);

        if (nhs is null)
        {
            throw new KeyNotFoundException("No exact match for supplied person specification.");
        }

        var enc = _encryptionResolver.ResolveForCaller(caller);
        var encrypted = _crypto.EncryptNhsToPersonId(nhs, enc);

        return new PersonMatch(encrypted);
    }

    public async Task<SearchJob> StartSearchAsync(DurableTaskClient durableClient, AuthContext caller, string personId, CancellationToken ct)
    {
        var callerEncryption = _encryptionResolver.ResolveForCaller(caller);
        var sui = _crypto.DecryptPersonIdToNhs(personId, callerEncryption);

        var policy = ToPolicyContext(caller);
        var jobId = Guid.NewGuid().ToString();

        await _core.StartSearchAsync(durableClient, jobId, sui, personId, policy, ct);

        var now = DateTimeOffset.UtcNow.ToString("O");
        var status = SearchStatus.Queued;

        return new SearchJob(
            JobId: jobId,
            PersonId: personId,
            Status: status,
            CreatedAt: now,
            LastUpdatedAt: null,
            _Links: BuildJobLinks(jobId, status)
        );
    }

    public async Task<SearchJob> GetStatusAsync(DurableTaskClient durableClient, AuthContext caller, string jobId, CancellationToken ct)
    {
        var (status, created, updated, searchMetadata) = await _core.GetRuntimeStatusAsync(durableClient, jobId, ct);

        return new SearchJob(
            JobId: jobId,
            PersonId: searchMetadata.PersonId,
            Status: status,
            CreatedAt: created.ToString("O"),
            LastUpdatedAt: updated.ToString("O"),
            _Links: BuildJobLinks(jobId, status)
        );
    }

    public async Task<SearchResults> GetResultsAsync(DurableTaskClient durableClient, AuthContext caller, string jobId, CancellationToken ct)
    {
        var (status, _, _, searchMetadata) = await _core.GetRuntimeStatusAsync(durableClient, jobId, ct);

        if (status != SearchStatus.Completed)
        {
            throw new InvalidOperationException("Results are only available once the search has completed.");
        }

        var raw = await _core.GetRawResultsAsync(durableClient, jobId, ct);

        var policy = ToPolicyContext(caller);
        var filtered = _pep.Filter(policy, raw);

        return new SearchResults(
            JobId: jobId,
            PersonId: searchMetadata.PersonId,
            Status: status,
            Items: filtered,
            _Links: BuildResultsLinks(jobId)
        );
    }

    public async Task<SearchJob> CancelAsync(DurableTaskClient durableClient, AuthContext caller, string jobId, CancellationToken ct)
    {
        var (currentStatus, _, _, _) = await _core.GetRuntimeStatusAsync(durableClient, jobId, ct);

        if (!CanCancel(currentStatus))
        {
            throw new InvalidOperationException("Search cannot be cancelled once it has completed, failed, or already been cancelled.");
        }

        await _core.CancelAsync(durableClient, jobId, ct);

        var (status, created, updated, searchMetadata) = await _core.GetRuntimeStatusAsync(durableClient, jobId, ct);

        return new SearchJob(
            JobId: jobId,
            PersonId: searchMetadata.PersonId,
            Status: status,
            CreatedAt: created.ToString("O"),
            LastUpdatedAt: updated.ToString("O"),
            _Links: BuildJobLinks(jobId, status)
        );
    }

    private static SearchJobLinks BuildJobLinks(string jobId, SearchStatus status)
    {
        var self = new HalLink($"/v1/searches/{jobId}", "GET");

        HalLink? statusLink = new HalLink($"/v1/searches/{jobId}", "GET");

        HalLink? resultsLink = status == SearchStatus.Completed
            ? new HalLink($"/v1/searches/{jobId}/results", "GET")
            : null;

        HalLink? cancelLink = CanCancel(status)
            ? new HalLink($"/v1/searches/{jobId}", "DELETE")
            : null;

        return new SearchJobLinks(
            Self: self,
            Status: statusLink,
            Results: resultsLink,
            Cancel: cancelLink
        );
    }

    private static SearchResultsLinks BuildResultsLinks(string jobId)
    {
        return new SearchResultsLinks(
            Self: new HalLink($"/v1/searches/{jobId}/results", "GET"),
            Job: new HalLink($"/v1/searches/{jobId}", "GET")
        );
    }

    private static bool CanCancel(SearchStatus status)
    {
        return status == SearchStatus.Queued || status == SearchStatus.Running;
    }

    private static PolicyContext ToPolicyContext(AuthContext caller)
    {
        return new PolicyContext(
            ClientId: caller.ClientId,
            Scopes: caller.Scopes
        );
    }
}
