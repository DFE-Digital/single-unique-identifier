using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class AggregatedDataRepository : IAggregatedDataRepository
{
    public Task AddOrUpdateAsync(AggregatedData aggregatedData) => Task.CompletedTask;
}
