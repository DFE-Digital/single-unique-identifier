using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.Services;

public class JobProcessorService(
    IJobRepository jobRepository,
    IWorkItemJobCountRepository workItemJobCountRepository,
    ILogger<JobProcessorService> logger,
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
            logger.LogWarning(
                "Job {JobId} not found for custodian {CustodianId}",
                jobId,
                custodianId
            );
            return null;
        }

        // the work item is currently leased
        if (job.LeaseId is null)
        {
            logger.LogWarning("Job {JobId} has no active lease", jobId);
            return null;
        }

        // the submitted leaseId matches the current lease
        if (job.LeaseId != leaseId)
        {
            logger.LogWarning(
                "LeaseId mismatch for Job {JobId}: expected {ExpectedLease}, received {SubmittedLease}",
                jobId,
                job.LeaseId,
                leaseId
            );
            return null;
        }

        // the lease is owned by the authenticated custodian
        if (job.CustodianId != custodianId)
        {
            logger.LogWarning(
                "Custodian mismatch for Job {JobId}: expected {ExpectedCustodian}, received {SubmittedCustodian}",
                jobId,
                job.CustodianId,
                custodianId
            );
            return null;
        }

        // the lease has not expired
        if (job.LeaseExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            logger.LogWarning(
                "Lease for Job {JobId} has expired at {LeaseExpires}",
                jobId,
                job.LeaseExpiresAtUtc
            );
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
            logger.LogWarning(
                "No job found with JobId {JobId} in current window for custodian {CustodianId}",
                jobId,
                custodianId
            );
            return;
        }

        job.CompletedAtUtc = DateTimeOffset.UtcNow;

        await jobRepository.UpsertAsync(job, cancellationToken);

        if (job.WorkItemId != null)
        {
            await workItemJobCountRepository.MarkJobCompletedAsync(
                job.WorkItemId,
                job.JobType,
                jobId,
                cancellationToken
            );
        }
    }
}
