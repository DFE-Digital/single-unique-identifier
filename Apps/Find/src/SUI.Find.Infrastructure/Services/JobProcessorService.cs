using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.JobRepository;

namespace SUI.Find.Infrastructure.Services;

public class JobProcessorService(
    IJobRepository jobRepository,
    IJobWindowStartService jobWindowStartService
) : IJobProcessorService
{
    public async Task<Job?> ValidateLeaseAsync(
        string jobId,
        string leaseId,
        string custodianId,
        CancellationToken cancellationToken
    )
    {
        var job = await GetJobByIdAndCustodianIdAsync(jobId, custodianId, cancellationToken);

        // the work item exists
        if (job is null)
        {
            return null;
        }

        // the work item is currently leased
        if (job.LeaseId is null)
        {
            return null;
        }

        // the submitted leaseId matches the current lease
        if (job.LeaseId != leaseId)
        {
            return null;
        }

        // the lease is owned by the authenticated custodian
        if (job.CustodianId != custodianId)
        {
            return null;
        }

        // the lease has not expired
        if (job.LeaseExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return job;
    }

    public async Task<Job?> GetJobByIdAndCustodianIdAsync(
        string jobId,
        string custodianId,
        CancellationToken cancellationToken
    )
    {
        var windowStart = jobWindowStartService.GetWindowStart();

        var jobs = await jobRepository.ListJobsByCustodianIdAsync(
            custodianId,
            windowStart,
            cancellationToken
        );

        var job = jobs.FirstOrDefault(j => j.JobId == jobId);

        return job;
    }

    public async Task MarkCompletedAsync(
        string jobId,
        string custodianId,
        CancellationToken cancellationToken
    )
    {
        var job = await GetJobByIdAndCustodianIdAsync(jobId, custodianId, cancellationToken);

        if (job is null)
        {
            return;
        }

        job.CompletedAtUtc = DateTimeOffset.UtcNow;

        await jobRepository.UpsertAsync(job, cancellationToken);
    }
}
