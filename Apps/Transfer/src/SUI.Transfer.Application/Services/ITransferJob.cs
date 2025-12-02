using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface ITransferJob
{
    Task<ConformedData> TransferAsync(Guid jobId, string sui);
}
