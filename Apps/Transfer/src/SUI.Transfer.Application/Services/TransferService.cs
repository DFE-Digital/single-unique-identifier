using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using static SUI.Transfer.Application.Services.ITransferService;

namespace SUI.Transfer.Application.Services;

public class TransferService(
    ITransferJob transferJob,
    IMemoryCache memoryCache,
    ILogger<TransferService> logger
) : ITransferService
{
    private const string JobsStateKey = "TransferJobsState";

    private Dictionary<Guid, TransferJobState> JobsState =>
        memoryCache.GetOrCreate(
            JobsStateKey,
            _ => new Dictionary<Guid, TransferJobState>(),
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1))
        )!;

    public QueuedTransferJobState BeginTransferJob(string sui)
    {
        var jobId = Guid.NewGuid();

        // Queue the transfer job to be run in the background.
        // This can be revisited in the future, if we need to use a more advanced scheduling/queueing approach.
        Task.Run(PerformTransferJobAsync);

        return new QueuedTransferJobState(jobId, sui);

        async Task PerformTransferJobAsync()
        {
            using var logScope = logger.BeginScope(
                "Starting transfer job for sui {Sui} (Job ID: {JobId})",
                sui,
                jobId
            );

            try
            {
                UpdateJobState(jobId, new RunningTransferJobState(jobId, sui));

                var aggregatedData = await transferJob.TransferAsync(jobId, sui);

                UpdateJobState(jobId, new CompletedTransferJobState(jobId, sui, aggregatedData));
            }
            catch (OperationCanceledException e)
            {
                logger.LogWarning(
                    e,
                    "Transfer job for sui {Sui} (Job ID: {JobId}) was canceled while running",
                    sui,
                    jobId
                );
                UpdateJobState(jobId, new CancelledTransferJobState(jobId, sui));
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Error occurred during transfer job for sui {Sui} (Job ID: {JobId})",
                    sui,
                    jobId
                );
                UpdateJobState(
                    jobId,
                    new FailedTransferJobState(jobId, sui, e.ToString(), e.StackTrace)
                );
            }
        }
    }

    public TransferJobState? GetTransferJobState(Guid jobId) => JobsState.GetValueOrDefault(jobId);

    private void UpdateJobState(Guid jobId, TransferJobState newState) =>
        JobsState[jobId] = newState;
}
