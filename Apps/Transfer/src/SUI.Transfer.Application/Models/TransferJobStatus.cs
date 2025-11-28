namespace SUI.Transfer.Application.Models;

public enum TransferJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
}
