using Azure;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Configuration;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Repositories.JobRepository;

namespace SUI.Find.Infrastructure.Services;

public class JobClaimService(
    IJobRepository jobRepository,
    IJobWindowStartService jobWindowStartService,
    IOptionsMonitor<JobClaimConfig> options,
    TimeProvider timeProvider,
    ILogger<JobClaimService> logger
) : IJobClaimService
{
    private JobClaimConfig JobClaimConfig => options.CurrentValue;

    public async Task<JobInfo?> ClaimNextAvailableJobAsync(string custodianId)
    {
        var retryCount = 0;
        do
        {
            var nextAvailableJob = await GetNextAvailableJobAsync(custodianId);
            if (nextAvailableJob == null)
            {
                return null;
            }

            var leaseId = Guid.NewGuid().ToString();
            var utcNow = timeProvider.GetUtcNow();
            var claimedJob = nextAvailableJob with
            {
                LeaseId = leaseId,
                LeaseExpiresAtUtc = utcNow.AddMinutes(JobClaimConfig.LeaseDurationMinutes),
                AttemptCount = nextAvailableJob.AttemptCount + 1,
                UpdatedAtUtc = utcNow,
            };

            var (sui, recordType) = ExtractSuiAndRecordTypeFromPayload(claimedJob);

            try
            {
                await jobRepository.UpdateAsync(claimedJob, nextAvailableJob.ETag);

                return new JobInfo
                {
                    JobId = claimedJob.JobId,
                    CustodianId = custodianId,
                    LeaseExpiresAtUtc = claimedJob.LeaseExpiresAtUtc.Value,
                    LeaseId = leaseId,
                    WorkItemId = claimedJob.WorkItemId,
                    Sui = sui,
                    RecordType = recordType,
                };
            }
            catch (RequestFailedException e) when (IsAlreadyClaimedConcurrentlyError(e))
            {
                // This invocation lost the race, but that is fine, another invocation won and that winner has carried on.
                // So, retry again to check to see whether there is another job to claim.
                logger.LogWarning(
                    e,
                    "Job {JobId} was already claimed concurrently",
                    claimedJob.JobId
                );
            }
        } while (++retryCount <= JobClaimConfig.MaxReScanAttempts);

        return null;
    }

    private async Task<Job?> GetNextAvailableJobAsync(string custodianId)
    {
        var utcNow = timeProvider.GetUtcNow();
        var windowStart = jobWindowStartService.GetWindowStart();

        return (await jobRepository.ListJobsByCustodianIdAsync(custodianId, windowStart))
            .OrderBy(job => job.CreatedAtUtc)
            .FirstOrDefault(job =>
                job.CompletedAtUtc == null
                && job.AttemptCount < JobClaimConfig.MaxClaimAttemptsPerJob
                && (job.LeaseExpiresAtUtc == null || job.LeaseExpiresAtUtc <= utcNow)
            );
    }

    private static (string? sui, string? recordType) ExtractSuiAndRecordTypeFromPayload(Job job)
    {
        if (job.JobType != JobType.CustodianLookup)
        {
            return (null, null);
        }

        // TODO: SUI-1545 - extract SUI & Record Type from Payload
        return ("", "");
    }

    /// <summary>
    /// Returns true if the specified error was caused because the requested entity has already been updated concurrently elsewhere by a different invocation.
    /// i.e. 412 (Precondition Failed)
    /// </summary>
    private static bool IsAlreadyClaimedConcurrentlyError(
        RequestFailedException requestFailedException
    ) =>
        requestFailedException.Status == 412
        && requestFailedException.ErrorCode == TableErrorCode.UpdateConditionNotSatisfied;
}
