using Microsoft.Extensions.Logging;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class TransferService(
    ITransferJob transferJob,
    ITransferJobStateRepository transferJobStateRepository,
    ILogger<TransferService> logger,
    TimeProvider timeProvider
) : ITransferService
{
    public QueuedTransferJobState BeginTransferJob(string sui)
    {
        var jobId = Guid.NewGuid();

        // Normalize the SUI
        sui = sui.ToUpperInvariant().Replace(" ", "");
        var jobState = new QueuedTransferJobState(jobId, sui, timeProvider.GetUtcNow());

        // Queue the transfer job to be run in the background.
        // This can be revisited in the future, if we need to use a more advanced scheduling/queueing approach.
        Task.Run(PerformTransferJobAsync);

        return jobState;

        async Task PerformTransferJobAsync()
        {
            using var logScope = logger.BeginScope(
                "Starting transfer job for sui {Sui} (Job ID: {JobId})",
                sui,
                jobId
            );

            try
            {
                await UpdateJobStateAsync(
                    TransferJobStateFactory.RunningJob(jobState, timeProvider.GetUtcNow())
                );

                var conformedData = await transferJob.TransferAsync(jobId, sui);

                await UpdateJobStateAsync(
                    TransferJobStateFactory.CompletedJob(
                        jobState,
                        conformedData,
                        timeProvider.GetUtcNow()
                    )
                );
            }
            catch (OperationCanceledException e)
            {
                logger.LogWarning(
                    e,
                    "Transfer job for sui {Sui} (Job ID: {JobId}) was canceled while running",
                    sui,
                    jobId
                );
                await UpdateJobStateAsync(
                    TransferJobStateFactory.CancelledJob(
                        jobState,
                        "Cancelled while running, due to host application shutdown",
                        timeProvider.GetUtcNow()
                    )
                );
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "Error occurred during transfer job for sui {Sui} (Job ID: {JobId})",
                    sui,
                    jobId
                );
                await UpdateJobStateAsync(
                    TransferJobStateFactory.FailedJob(
                        jobState,
                        e.ToString(),
                        e.StackTrace ?? string.Empty,
                        timeProvider.GetUtcNow()
                    )
                );
            }
        }
    }

    public async Task<TransferJobState?> GetTransferJobStateAsync(Guid jobId) =>
        await transferJobStateRepository.GetAsync(jobId);

    private async Task UpdateJobStateAsync(TransferJobState newState) =>
        await transferJobStateRepository.AddOrUpdateAsync(newState);
}
