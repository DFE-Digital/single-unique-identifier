using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Interfaces;

public interface IJobClaimService
{
    Task<JobInfo?> ClaimNextAvailableJobAsync(
        string custodianId,
        CancellationToken cancellationToken
    );

    Task<bool> DoesCustodianHaveJobs(string custodianId, CancellationToken cancellationToken);

    Task<JobInfo?> ExtendJobLeaseAsync(
        string custodianId,
        string jobId,
        string leaseId,
        CancellationToken cancellationToken
    );
}
