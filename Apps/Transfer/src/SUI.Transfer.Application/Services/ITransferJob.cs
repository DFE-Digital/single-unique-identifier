using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface ITransferJob
{
    Task<AggregatedConsolidatedData> TransferAsync(Guid jobId, string sui);
}
