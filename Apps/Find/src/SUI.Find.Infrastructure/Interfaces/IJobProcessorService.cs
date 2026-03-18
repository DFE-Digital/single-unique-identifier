using SUI.Find.Infrastructure.Repositories.JobRepository;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IJobProcessorService
{
    Task<Job?> ValidateLeaseAsync(
        string jobId,
        string leaseId,
        string custodianId,
        CancellationToken cancellationToken
    );

    Task<Job?> GetJobByIdAndCustodianIdAsync(
        string jobId,
        string custodianId,
        CancellationToken cancellationToken
    );

    Task MarkCompletedAsync(string jobId, string custodianId, CancellationToken cancellationToken);
}
