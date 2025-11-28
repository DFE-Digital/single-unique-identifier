using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Application.Services;

public interface ITransferService
{
    QueuedTransferJobState BeginTransferJob(string sui);

    TransferJobState? GetTransferJobState(Guid jobId);

    public abstract record TransferJobState(Guid JobId, string Sui, TransferJobStatus Status);

    public record QueuedTransferJobState(Guid JobId, string Sui)
        : TransferJobState(JobId, Sui, TransferJobStatus.Queued);

    public record RunningTransferJobState(Guid JobId, string Sui)
        : TransferJobState(JobId, Sui, TransferJobStatus.Running);

    // rs-todo: add consolidated record, and provider errors
    public record CompletedTransferJobState(Guid JobId, string Sui)
        : TransferJobState(JobId, Sui, TransferJobStatus.Completed);

    public record FailedTransferJobState(
        Guid JobId,
        string Sui,
        string ErrorMessage,
        string? StackTrace
    ) : TransferJobState(JobId, Sui, TransferJobStatus.Failed);

    public record CancelledTransferJobState(Guid JobId, string Sui)
        : TransferJobState(JobId, Sui, TransferJobStatus.Canceled);
}
