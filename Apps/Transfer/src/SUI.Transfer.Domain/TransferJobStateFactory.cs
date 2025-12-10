namespace SUI.Transfer.Domain;

public static class TransferJobStateFactory
{
    public static RunningTransferJobState RunningJob(
        TransferJobState jobState,
        DateTimeOffset lastUpdatedAt
    )
    {
        return new RunningTransferJobState(
            jobState.JobId,
            jobState.Sui,
            jobState.CreatedAt,
            lastUpdatedAt
        );
    }

    public static CancelledTransferJobState CancelledJob(
        TransferJobState jobState,
        string cancellationReason,
        DateTimeOffset lastUpdatedAt
    )
    {
        return new CancelledTransferJobState(
            jobState.JobId,
            jobState.Sui,
            cancellationReason,
            jobState.CreatedAt,
            lastUpdatedAt
        );
    }

    public static CompletedTransferJobState CompletedJob(
        TransferJobState jobState,
        ConformedData conformedData,
        DateTimeOffset lastUpdatedAt
    )
    {
        return new CompletedTransferJobState(
            jobState.JobId,
            jobState.Sui,
            conformedData,
            jobState.CreatedAt,
            lastUpdatedAt
        );
    }

    public static FailedTransferJobState FailedJob(
        TransferJobState jobState,
        string errorMessage,
        string stackTrace,
        DateTimeOffset lastUpdatedAt
    )
    {
        return new FailedTransferJobState(
            jobState.JobId,
            jobState.Sui,
            errorMessage,
            stackTrace,
            jobState.CreatedAt,
            lastUpdatedAt
        );
    }
}
