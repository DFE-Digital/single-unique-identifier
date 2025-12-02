using SUI.Transfer.Application.Models;

namespace SUI.Transfer.Domain;

public abstract record TransferJobState(Guid JobId, string Sui, TransferJobStatus Status)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record QueuedTransferJobState(Guid JobId, string Sui)
    : TransferJobState(JobId, Sui, TransferJobStatus.Queued);

public record RunningTransferJobState(Guid JobId, string Sui)
    : TransferJobState(JobId, Sui, TransferJobStatus.Running);

public record CompletedTransferJobState(Guid JobId, string Sui, ConformedData ConformedData)
    : TransferJobState(JobId, Sui, TransferJobStatus.Completed);

public record FailedTransferJobState(
    Guid JobId,
    string Sui,
    string ErrorMessage,
    string? StackTrace
) : TransferJobState(JobId, Sui, TransferJobStatus.Failed);

public record CancelledTransferJobState(Guid JobId, string Sui, string CancellationReason)
    : TransferJobState(JobId, Sui, TransferJobStatus.Canceled);
