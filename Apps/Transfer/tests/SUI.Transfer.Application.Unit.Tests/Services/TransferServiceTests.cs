using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Transfer.Application.Models;
using SUI.Transfer.Application.Services;
using static SUI.Transfer.Application.Services.ITransferService;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public sealed class TransferServiceTests : IDisposable
{
    private readonly ITransferJob _mockTransferJob = Substitute.For<ITransferJob>();
    private readonly MemoryCache _testMemoryCache = new(new MemoryCacheOptions());
    private readonly ILogger<TransferService> _mockLogger = Substitute.For<
        ILogger<TransferService>
    >();

    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        _sut = new TransferService(_mockTransferJob, _testMemoryCache, _mockLogger);
    }

    public void Dispose()
    {
        _testMemoryCache.Dispose();
    }

    [Fact]
    public async Task Transfer_HappyPath_Test()
    {
        // Arrange
        const string requestId = "999-000-1234";

        var mutex = new SemaphoreSlim(0, 1);
        _mockTransferJob
            .When(x => x.TransferAsync(Arg.Any<Guid>(), requestId))
            .Do(i =>
            {
                var jobId = i.Arg<Guid>();

                // Verify that the intermediate state is as expected
                var intermediateResult = _sut.GetTransferJobState(jobId);
                Assert.Equal(new RunningTransferJobState(jobId, requestId), intermediateResult);
            });

        var mockJobScope = Substitute.For<IDisposable>();
        mockJobScope
            .When(x => x.Dispose())
            .Do(_ =>
            {
                // Let the test continue
                mutex.Release();
            });

        _mockLogger.BeginScope(Arg.Any<object>()).Returns(mockJobScope);

        // Act
        var initialResult = _sut.BeginTransferJob(requestId);

        Assert.True(await mutex.WaitAsync(TimeSpan.FromSeconds(5)));

        // Assert - initial result should be Queued
        Assert.NotNull(initialResult);
        Assert.Equal(new QueuedTransferJobState(initialResult.JobId, requestId), initialResult);

        // Assert - transfer job should have been called as expected
        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, requestId);

        // Assert - final state should be Completed
        var finalResult = _sut.GetTransferJobState(initialResult.JobId);
        Assert.Equal(new CompletedTransferJobState(initialResult.JobId, requestId), finalResult);
    }

    [Fact]
    public async Task Transfer_FailurePath_Test()
    {
        // Arrange
        const string requestId = "999-000-1234";

        var mutex = new SemaphoreSlim(0, 1);
        _mockTransferJob
            .When(x => x.TransferAsync(Arg.Any<Guid>(), requestId))
            .Throw(new Exception("Mock error"));

        var mockJobScope = Substitute.For<IDisposable>();
        mockJobScope
            .When(x => x.Dispose())
            .Do(_ =>
            {
                // Let the test continue
                mutex.Release();
            });

        _mockLogger.BeginScope(Arg.Any<object>()).Returns(mockJobScope);

        // Act
        var initialResult = _sut.BeginTransferJob(requestId);

        Assert.True(await mutex.WaitAsync(TimeSpan.FromSeconds(5)));

        // Assert - initial result should be Queued
        Assert.NotNull(initialResult);
        Assert.Equal(new QueuedTransferJobState(initialResult.JobId, requestId), initialResult);

        // Assert - transfer job should have been called as expected
        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, requestId);

        // Assert - final state should be Failed
        var finalResult = _sut.GetTransferJobState(initialResult.JobId);
        Assert.NotNull(finalResult);
        Assert.Equal(TransferJobStatus.Failed, finalResult.Status);
        Assert.Equal(initialResult.JobId, finalResult.JobId);
        Assert.Equal(requestId, finalResult.Sui);
    }

    [Fact]
    public async Task Transfer_CancellationPath_Test()
    {
        // Arrange
        const string requestId = "999-000-1234";

        var mutex = new SemaphoreSlim(0, 1);
        _mockTransferJob
            .When(x => x.TransferAsync(Arg.Any<Guid>(), requestId))
            .Throw(new OperationCanceledException("Mock cancellation"));

        var mockJobScope = Substitute.For<IDisposable>();
        mockJobScope
            .When(x => x.Dispose())
            .Do(_ =>
            {
                // Let the test continue
                mutex.Release();
            });

        _mockLogger.BeginScope(Arg.Any<object>()).Returns(mockJobScope);

        // Act
        var initialResult = _sut.BeginTransferJob(requestId);

        Assert.True(await mutex.WaitAsync(TimeSpan.FromSeconds(5)));

        // Assert - initial result should be Queued
        Assert.NotNull(initialResult);
        Assert.Equal(new QueuedTransferJobState(initialResult.JobId, requestId), initialResult);

        // Assert - transfer job should have been called as expected
        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, requestId);

        // Assert - final state should be Canceled
        var finalResult = _sut.GetTransferJobState(initialResult.JobId);
        Assert.NotNull(finalResult);
        Assert.Equal(TransferJobStatus.Canceled, finalResult.Status);
        Assert.Equal(initialResult.JobId, finalResult.JobId);
        Assert.Equal(requestId, finalResult.Sui);
    }
}
