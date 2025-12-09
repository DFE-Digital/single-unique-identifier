using Microsoft.Extensions.Caching.Memory;
using SUI.Transfer.Domain;
using SUI.Transfer.Infrastructure.Repositories;

namespace SUI.Transfer.Infrastructure.Unit.Tests.Repositories;

public sealed class TransferJobStateMemoryCacheRepositoryTests : IDisposable
{
    private readonly MemoryCache _testMemoryCache = new(new MemoryCacheOptions());

    private readonly TransferJobStateMemoryCacheRepository _sut;

    public TransferJobStateMemoryCacheRepositoryTests() =>
        _sut = new TransferJobStateMemoryCacheRepository(_testMemoryCache);

    public void Dispose()
    {
        _testMemoryCache.Dispose();
    }

    [Fact]
    public async Task AddOrUpdateAsync_Adds_IfNotAlreadyPresent()
    {
        var jobState = new QueuedTransferJobState(
            Guid.NewGuid(),
            "xyz",
            TimeProvider.System.GetUtcNow()
        );

        // ACT
        await _sut.AddOrUpdateAsync(jobState);

        // ASSERT
        var result = await _sut.GetAsync(jobState.JobId);
        result.Should().BeSameAs(jobState);
    }

    [Fact]
    public async Task AddOrUpdateAsync_Updates_IfAlreadyPresent()
    {
        var jobId = Guid.NewGuid();
        var createdAt = TimeProvider.System.GetUtcNow();
        var jobState1 = new QueuedTransferJobState(jobId, "xyz", createdAt);

        await _sut.AddOrUpdateAsync(jobState1);

        var jobState2 = TransferJobStateFactory.RunJob(jobState1, createdAt);

        // ACT
        await _sut.AddOrUpdateAsync(jobState2);

        // ASSERT
        var result = await _sut.GetAsync(jobId);
        result.Should().BeSameAs(jobState2);
    }

    [Fact]
    public async Task GetAsync_Returns_Null_WhenNoMatchingJob()
    {
        // ACT
        var result = await _sut.GetAsync(Guid.NewGuid());

        // ASSERT
        result.Should().BeNull();
    }
}
