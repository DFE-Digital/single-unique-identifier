using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Transfer.Application.Models;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;
using Xunit.Abstractions;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class TransferServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ITransferJob _mockTransferJob = Substitute.For<ITransferJob>();
    private readonly ITransferJobStateRepository _stubTransferJobStateRepository =
        new StubTransferJobStateRepository();
    private readonly ILogger<TransferService> _mockLogger = Substitute.For<
        ILogger<TransferService>
    >();

    private readonly TransferService _sut;

    public TransferServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _sut = new TransferService(_mockTransferJob, _stubTransferJobStateRepository, _mockLogger);
    }

    private class StubTransferJobStateRepository : ITransferJobStateRepository
    {
        private readonly Dictionary<Guid, TransferJobState> _jobStates = new();

        public Task AddOrUpdateAsync(TransferJobState transferJobState)
        {
            _jobStates[transferJobState.JobId] = transferJobState;
            return Task.CompletedTask;
        }

        public Task<TransferJobState?> GetAsync(Guid jobId) =>
            Task.FromResult(_jobStates.GetValueOrDefault(jobId));
    }

    private static ConformedData CreateEmptyConformedConsolidatedData(Guid jobId, string sui) =>
        new(
            jobId,
            new ConsolidatedData(sui)
            {
                ChildPersonalDetailsRecord = new(),
                ChildSocialCareDetailsRecord = new(),
                EducationDetailsRecord = new(),
                ChildHealthDataRecord = new(),
                ChildLinkedCrimeDataRecord = new(),
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        )
        {
            EducationAttendanceSummaries = null,
            HealthAttendanceSummaries = null,
            ChildrensSocialCareReferralSummaries = null,
            CrimeMissingEpisodesPast6Months = null,
        };

    [Fact]
    public async Task Transfer_HappyPath_Test()
    {
        // Arrange
        const string requestId = "999-000-1234";

        var mutex = new SemaphoreSlim(0, 1);
        _mockTransferJob
            .TransferAsync(Arg.Any<Guid>(), requestId)
            .Returns(async callInfo =>
            {
                var jobId = callInfo.Arg<Guid>();

                // Verify that the intermediate state is as expected
                var intermediateResult = await _sut.GetTransferJobStateAsync(jobId);
                Assert.NotNull(intermediateResult);
                Assert.Equal(
                    new RunningTransferJobState(jobId, requestId, intermediateResult.CreatedAt)
                    {
                        LastUpdatedAt = intermediateResult.LastUpdatedAt,
                    },
                    intermediateResult
                );

                return CreateEmptyConformedConsolidatedData(jobId, requestId);
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
        Assert.Equal(
            new QueuedTransferJobState(initialResult.JobId, requestId, initialResult.CreatedAt)
            {
                LastUpdatedAt = initialResult.LastUpdatedAt,
            },
            initialResult
        );

        // Assert - transfer job should have been called as expected
        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, requestId);

        // Assert - final state should be Completed
        var finalResult = await _sut.GetTransferJobStateAsync(initialResult.JobId);
        _testOutputHelper.WriteLine(finalResult?.ToString());

        finalResult
            .Should()
            .BeEquivalentTo(
                new CompletedTransferJobState(
                    initialResult.JobId,
                    requestId,
                    CreateEmptyConformedConsolidatedData(initialResult.JobId, requestId),
                    initialResult.CreatedAt
                ),
                options =>
                    options
                        .Excluding(x => x.LastUpdatedAt)
                        .Excluding(x => x.CreatedAt)
                        .Excluding(x => x.ConformedData.CreatedDate)
            );
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
        Assert.Equal(
            new QueuedTransferJobState(initialResult.JobId, requestId, initialResult.CreatedAt)
            {
                LastUpdatedAt = initialResult.LastUpdatedAt,
            },
            initialResult
        );

        // Assert - transfer job should have been called as expected
        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, requestId);

        // Assert - final state should be Failed
        var finalResult = await _sut.GetTransferJobStateAsync(initialResult.JobId);
        Assert.NotNull(finalResult);
        Assert.Equal(TransferJobStatus.Failed, finalResult.Status);
        Assert.Equal(initialResult.JobId, finalResult.JobId);
        Assert.Equal(requestId, finalResult.Sui);

        var failedTransferJobState = Assert.IsType<FailedTransferJobState>(finalResult);
        Assert.NotNull(failedTransferJobState.ErrorMessage);
        Assert.NotNull(failedTransferJobState.StackTrace);
        Assert.Contains("Mock error", failedTransferJobState.ErrorMessage);
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
        Assert.Equal(
            new QueuedTransferJobState(initialResult.JobId, requestId, initialResult.CreatedAt)
            {
                LastUpdatedAt = initialResult.LastUpdatedAt,
            },
            initialResult
        );

        // Assert - transfer job should have been called as expected
        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, requestId);

        // Assert - final state should be CancelLed
        var finalResult = await _sut.GetTransferJobStateAsync(initialResult.JobId);
        Assert.NotNull(finalResult);
        var cancelledJobState = Assert.IsType<CancelledTransferJobState>(finalResult);
        Assert.Equal(TransferJobStatus.Canceled, finalResult.Status);
        Assert.Equal(
            "Cancelled while running, due to host application shutdown",
            cancelledJobState.CancellationReason
        );
        Assert.Equal(initialResult.JobId, finalResult.JobId);
        Assert.Equal(requestId, finalResult.Sui);
    }

    [Theory]
    [InlineData("aBc 123", "ABC123")]
    [InlineData("999 0000 111", "9990000111")]
    public async Task Transfer_Does_Normalize_SUI(string inputSui, string expectedNormalizedSui)
    {
        var mutex = new SemaphoreSlim(0, 1);

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
        var initialResult = _sut.BeginTransferJob(inputSui);

        // Assert
        Assert.True(await mutex.WaitAsync(TimeSpan.FromSeconds(5)));

        var finalResult = await _sut.GetTransferJobStateAsync(initialResult.JobId);

        Assert.IsType<CompletedTransferJobState>(finalResult);

        await _mockTransferJob.Received().TransferAsync(initialResult.JobId, expectedNormalizedSui);
    }
}
