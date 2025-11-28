using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Transfer.Application.Services;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class TransferJobTests
{
    private readonly IRecordFinder _mockRecordFinder = Substitute.For<IRecordFinder>();
    private readonly IRecordFetcher _mockRecordFetcher = Substitute.For<IRecordFetcher>();
    private readonly IRecordConsolidator _mockRecordConsolidator =
        Substitute.For<IRecordConsolidator>();
    private readonly IConsolidatedDataAggregator _mockConsolidatedDataAggregator =
        Substitute.For<IConsolidatedDataAggregator>();
    private readonly IHostApplicationLifetime _mockHostApplicationLifetime =
        Substitute.For<IHostApplicationLifetime>();
    private readonly ILogger<TransferJob> _mockLogger = Substitute.For<ILogger<TransferJob>>();

    private readonly TransferJob _sut;

    public TransferJobTests()
    {
        _sut = new TransferJob(
            _mockRecordFinder,
            _mockRecordFetcher,
            _mockRecordConsolidator,
            _mockConsolidatedDataAggregator,
            _mockHostApplicationLifetime,
            _mockLogger
        );
    }

    [Fact]
    public async Task TransferAsync_Does_ThrowIfCancellationRequested()
    {
        using var cts = new CancellationTokenSource();

        _mockHostApplicationLifetime.ApplicationStopping.Returns(cts.Token);

        await cts.CancelAsync();

        // ACT & ASSERT
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _sut.TransferAsync(Guid.NewGuid(), "")
        );
    }

    // rs-todo: tests for TransferAsync
}
