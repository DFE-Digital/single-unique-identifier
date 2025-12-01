using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface ITransferJob
{
    Task<AggregatedData> TransferAsync(Guid jobId, string sui);
}
