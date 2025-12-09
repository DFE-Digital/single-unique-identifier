using Microsoft.Extensions.Logging;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class TransferService(
    ITransferJob transferJob,
    ITransferJobStateRepository transferJobStateRepository,
    ILogger<TransferService> logger
) : ITransferService
{
    public QueuedTransferJobState BeginTransferJob(string sui)
    {
        var jobId = Guid.NewGuid();
        var createdAt = TimeProvider.System.GetUtcNow();

        // Normalize the SUI
        sui = sui.ToUpperInvariant().Replace(" ", "");

        // Queue the transfer job to be run in the background.
        // This can be revisited in the future, if we need to use a more advanced scheduling/queueing approach.
        Task.Run(PerformTransferJobAsync);

        return new QueuedTransferJobState(jobId, sui, createdAt);

        async Task PerformTransferJobAsync()
        {
            using var logScope = logger.BeginScope(
                "Starting transfer job for sui {Sui} (Job ID: {JobId})",
                sui,
                jobId
            );

            try
            {
                await UpdateJobStateAsync(new RunningTransferJobState(jobId, sui, createdAt));

                var conformedData = await transferJob.TransferAsync(jobId, sui);

                await UpdateJobStateAsync(
                    new CompletedTransferJobState(jobId, sui, conformedData, createdAt)
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
                    new CancelledTransferJobState(
                        jobId,
                        sui,
                        "Cancelled while running, due to host application shutdown",
                        createdAt
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
                    new FailedTransferJobState(jobId, sui, e.ToString(), e.StackTrace, createdAt)
                );
            }
        }
    }

    public async Task<TransferJobState?> GetTransferJobStateAsync(Guid jobId) =>
        await transferJobStateRepository.GetAsync(jobId);

    private async Task UpdateJobStateAsync(TransferJobState newState) =>
        await transferJobStateRepository.AddOrUpdateAsync(newState);
}
