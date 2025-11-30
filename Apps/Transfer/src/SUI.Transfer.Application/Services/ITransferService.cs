using SUI.Transfer.Application.Models;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface ITransferService
{
    QueuedTransferJobState BeginTransferJob(string sui);

    public TransferJobState? GetTransferJobState(Guid jobId);
}
