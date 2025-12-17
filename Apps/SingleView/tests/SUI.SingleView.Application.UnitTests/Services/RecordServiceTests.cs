using Bogus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SUI.SingleView.Application.Exceptions;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Application.Services;
using SUI.SingleView.Domain.UnitTests.Extensions;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.UnitTests.Services;

public sealed class RecordServiceTests
{
    private readonly ITransferApi _mockTransferApi = Substitute.For<ITransferApi>();
    private readonly IPersonMapper _mockPersonMapper = Substitute.For<IPersonMapper>();
    private readonly IOptions<HttpPollingOptions> _mockHttpPollingOptions = Substitute.For<
        IOptions<HttpPollingOptions>
    >();
    private readonly ILogger<RecordService> _mockLogger = Substitute.For<ILogger<RecordService>>();

    private class IntervalFakeTimeProvider(Func<TimeSpan> getInterval) : FakeTimeProvider
    {
        public int CountOfTimersCreated { get; private set; }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period
        )
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            CountOfTimersCreated++;
            Advance(getInterval());
            return timer;
        }
    }

    private readonly IntervalFakeTimeProvider _fakeTimeProvider;
    private readonly Faker _faker = new("en_GB");
    private readonly string _nhsNumber;

    private readonly RecordService _sut;

    public RecordServiceTests()
    {
        _fakeTimeProvider = new IntervalFakeTimeProvider(() =>
            _mockHttpPollingOptions.Value.PollInterval
        );

        _sut = new RecordService(
            _mockTransferApi,
            _mockPersonMapper,
            _mockHttpPollingOptions,
            _fakeTimeProvider,
            _mockLogger
        );

        _nhsNumber = _faker.GenerateNhsNumber().Value;

        _mockHttpPollingOptions.Value.Returns(_ => new HttpPollingOptions
        {
            PollInterval = TimeSpan.FromSeconds(15),
            PollTimeout = TimeSpan.FromMinutes(2),
        });
    }

    [Fact]
    public async Task GetRecordAsync_DoesPoll_And_WhenJobBecomes_Completed_DoesReturnResult()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _mockTransferApi
            .TransferPOSTAsync(_nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new QueuedTransferJobState { JobId = jobId });

        _mockTransferApi
            .TransferGETAsync(jobId, TestContext.Current.CancellationToken)
            .Returns(
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Queued },
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Running },
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Running },
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Completed }
            );

        var conformedData = new ConformedData();
        _mockTransferApi
            .ResultsAsync(jobId, TestContext.Current.CancellationToken)
            .Returns(
                new CompletedTransferJobState
                {
                    JobId = jobId,
                    Status = TransferJobStatus.Completed,
                    Data = conformedData,
                }
            );

        var personModel = new PersonModel();
        _mockPersonMapper.Map(_nhsNumber, conformedData).Returns(personModel);

        // Act
        var result = await _sut.GetRecordAsync(_nhsNumber, TestContext.Current.CancellationToken);

        // Assert
        _mockPersonMapper.Received().Map(_nhsNumber, conformedData);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<PersonModel>();
        result.ShouldBe(personModel);

        await _mockTransferApi
            .Received()
            .TransferPOSTAsync(_nhsNumber, TestContext.Current.CancellationToken);
        await _mockTransferApi
            .Received()
            .TransferGETAsync(jobId, TestContext.Current.CancellationToken);
        await _mockTransferApi
            .Received()
            .ResultsAsync(jobId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecordAsync_WhenErrorOccurs_Logs_And_Throws_RecordException()
    {
        // Arrange
        var exampleException = new InvalidOperationException("example error");
        _mockTransferApi
            .TransferPOSTAsync(_nhsNumber, TestContext.Current.CancellationToken)
            .Throws(exampleException);

        // Act
        var actualException = await Assert.ThrowsAsync<RecordException>(async () =>
            await _sut.GetRecordAsync(_nhsNumber, TestContext.Current.CancellationToken)
        );

        // Assert
        actualException.Message.ShouldBe(
            $"An error occurred when trying to get the record for {_nhsNumber}"
        );
        actualException.InnerException.ShouldBe(exampleException);

        var call = _mockLogger
            .ReceivedCalls()
            .FirstOrDefault(c =>
                c.GetMethodInfo().Name == "Log"
                && (LogLevel)c.GetArguments()[0]! == LogLevel.Warning
            );

        call.ShouldNotBeNull();
        call.GetArguments()[2]!
            .ToString()
            .ShouldBe($"An error occurred when trying to get the record for {_nhsNumber}");
        call.GetArguments()[3].ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task GetRecordAsync_WhenCancellationRequested_DoesThrowAsExpected()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _mockTransferApi
            .TransferPOSTAsync(_nhsNumber, cts.Token)
            .Returns(new QueuedTransferJobState());

        // Act
        var actualException = await Assert.ThrowsAsync<RecordException>(async () =>
            await _sut.GetRecordAsync(_nhsNumber, cts.Token)
        );

        // Assert
        actualException.Message.ShouldBe(
            $"An error occurred when trying to get the record for {_nhsNumber}"
        );
        actualException.InnerException.ShouldBeOfType<OperationCanceledException>();
    }

    [Theory]
    [InlineData(TransferJobStatus.Canceled)]
    [InlineData(TransferJobStatus.Failed)]
    public async Task GetRecordAsync_WhenJobDoesNotCompleteOk_RecordException_IsThrown(
        TransferJobStatus finalStatus
    )
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _mockTransferApi
            .TransferPOSTAsync(_nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new QueuedTransferJobState { JobId = jobId });

        _mockTransferApi
            .TransferGETAsync(jobId, TestContext.Current.CancellationToken)
            .Returns(
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Queued },
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Running },
                new TransferJobState { JobId = jobId, Status = finalStatus }
            );

        // Act
        var actualException = await Assert.ThrowsAsync<RecordException>(async () =>
            await _sut.GetRecordAsync(_nhsNumber, TestContext.Current.CancellationToken)
        );

        // Assert
        actualException.Message.ShouldBe(
            $"An error occurred when trying to get the record for {_nhsNumber}"
        );
        actualException.InnerException.ShouldNotBeNull();
        actualException.InnerException.Message.ShouldBe(
            $"Transfer job {jobId} did not complete as expected. Status was: {finalStatus}"
        );
    }

    [Fact]
    public async Task GetRecordAsync_WhenJobDoesNotFinishWithinAllottedTime_RecordException_IsThrown()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _mockTransferApi
            .TransferPOSTAsync(_nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new QueuedTransferJobState { JobId = jobId });

        _mockTransferApi
            .TransferGETAsync(jobId, TestContext.Current.CancellationToken)
            .Returns(_ =>
            {
                _fakeTimeProvider.Advance(TimeSpan.FromHours(1));
                return new TransferJobState { JobId = jobId, Status = TransferJobStatus.Running };
            });

        // Act
        var actualException = await Assert.ThrowsAsync<RecordException>(async () =>
            await _sut.GetRecordAsync(_nhsNumber, TestContext.Current.CancellationToken)
        );

        // Assert
        actualException.Message.ShouldBe(
            $"An error occurred when trying to get the record for {_nhsNumber}"
        );
        actualException.InnerException.ShouldNotBeNull();
        actualException.InnerException.Message.ShouldStartWith(
            $"Transfer job {jobId} did not finish within allotted time"
        );
    }

    [Fact]
    public async Task GetRecordAsync_Does_Delay_BetweenPolling()
    {
        // Arrange
        var expectedMaxPollCount = (int)(
            _mockHttpPollingOptions.Value.PollTimeout / _mockHttpPollingOptions.Value.PollInterval
        );

        var jobId = Guid.NewGuid();

        _mockTransferApi
            .TransferPOSTAsync(_nhsNumber, TestContext.Current.CancellationToken)
            .Returns(new QueuedTransferJobState { JobId = jobId });

        _mockTransferApi
            .TransferGETAsync(jobId, TestContext.Current.CancellationToken)
            .Returns(
                new TransferJobState { JobId = jobId, Status = TransferJobStatus.Queued },
                [
                    .. Enumerable.Repeat(
                        new TransferJobState { JobId = jobId, Status = TransferJobStatus.Running },
                        expectedMaxPollCount - 2
                    ),
                    new TransferJobState { JobId = jobId, Status = TransferJobStatus.Completed },
                ]
            );

        var conformedData = new ConformedData();
        _mockTransferApi
            .ResultsAsync(jobId, TestContext.Current.CancellationToken)
            .Returns(
                new CompletedTransferJobState
                {
                    JobId = jobId,
                    Status = TransferJobStatus.Completed,
                    Data = conformedData,
                }
            );

        var personModel = new PersonModel();
        _mockPersonMapper.Map(_nhsNumber, conformedData).Returns(personModel);

        // Act
        await _sut.GetRecordAsync(_nhsNumber, TestContext.Current.CancellationToken);

        // Assert
        _fakeTimeProvider.CountOfTimersCreated.ShouldBeInRange(
            2,
            expectedMaxPollCount,
            "It should delay between polling"
        );
    }
}
