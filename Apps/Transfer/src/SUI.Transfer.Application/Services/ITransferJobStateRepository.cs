using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface ITransferJobStateRepository
{
    Task AddOrUpdateAsync(TransferJobState transferJobState);

    Task<TransferJobState?> GetAsync(Guid jobId);
}
