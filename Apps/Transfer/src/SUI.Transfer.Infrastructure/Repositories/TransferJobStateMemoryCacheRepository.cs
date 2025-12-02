using Microsoft.Extensions.Caching.Memory;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Infrastructure.Repositories;

public class TransferJobStateMemoryCacheRepository(IMemoryCache memoryCache)
    : ITransferJobStateRepository
{
    private const string JobsStateKey = "TransferJobsState";

    private Dictionary<Guid, TransferJobState> JobsState =>
        memoryCache.GetOrCreate(
            JobsStateKey,
            _ => new Dictionary<Guid, TransferJobState>(),
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1))
        )!;

    public Task AddOrUpdateAsync(TransferJobState transferJobState)
    {
        JobsState[transferJobState.JobId] = transferJobState;
        return Task.CompletedTask;
    }

    public Task<TransferJobState?> GetAsync(Guid jobId) =>
        Task.FromResult(JobsState.GetValueOrDefault(jobId));
}
