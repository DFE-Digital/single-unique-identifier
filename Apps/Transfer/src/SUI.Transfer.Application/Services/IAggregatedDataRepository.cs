using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IAggregatedDataRepository
{
    Task AddOrUpdateAsync(AggregatedData aggregatedData);
}
