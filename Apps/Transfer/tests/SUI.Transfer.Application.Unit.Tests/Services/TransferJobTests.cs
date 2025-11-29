using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class TransferJobTests
{
    private readonly IRecordFinder _mockRecordFinder = Substitute.For<IRecordFinder>();
    private readonly IRecordFetcher _mockRecordFetcher = Substitute.For<IRecordFetcher>();
    private readonly IRecordConsolidator _mockRecordConsolidator =
        Substitute.For<IRecordConsolidator>();
    private readonly IConsolidatedDataAggregator _mockConsolidatedDataAggregator =
        Substitute.For<IConsolidatedDataAggregator>();
    private readonly IAggregatedDataRepository _mockAggregatedDataRepository =
        Substitute.For<IAggregatedDataRepository>();
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
            _mockAggregatedDataRepository,
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

    [Fact]
    public async Task TransferAsync_Does_Flow_AsExpected()
    {
        const string sui = "999 123 4566";
        var jobId = Guid.NewGuid();

        RecordPointer[] mockRecordPointers = [];

        _mockRecordFinder
            .FindRecordsAsync(sui, Arg.Any<CancellationToken>())
            .Returns(mockRecordPointers);

        UnconsolidatedData mockUnconsolidatedData = new(sui)
        {
            ChildPersonalDetailsRecords = [],
            ChildSocialCareDetailsRecords = [],
            EducationDetailsRecords = [],
            ChildHealthDataRecords = [],
            ChildLinkedCrimeDataRecords = [],
            FailedFetches = [],
        };

        _mockRecordFetcher
            .FetchRecordsAsync(sui, mockRecordPointers, Arg.Any<CancellationToken>())
            .Returns(mockUnconsolidatedData);

        ConsolidatedData mockConsolidatedData = new(sui)
        {
            ChildPersonalDetailsRecord = null,
            ChildSocialCareDetailsRecord = null,
            EducationDetailsRecord = null,
            ChildHealthDataRecord = null,
            ChildLinkedCrimeDataRecord = null,
            CountOfRecordsSuccessfullyFetched = 0,
            FailedFetches = [],
        };

        _mockRecordConsolidator
            .ConsolidateRecords(mockUnconsolidatedData)
            .Returns(mockConsolidatedData);

        AggregatedData mockAggregatedData = new(jobId, mockConsolidatedData)
        {
            EducationAttendanceCurrentAcademicYear = null,
            EducationAttendanceLastAcademicYear = null,
            HealthAttendanceSummaryLast12Months = null,
            HealthAttendanceSummaryLast5Years = null,
            CSCReferralSummaryPast6Months = [],
            CSCReferralSummaryPast12Months = [],
            CSCReferralSummaryPast5Years = [],
        };

        _mockConsolidatedDataAggregator
            .ApplyAggregations(jobId, mockConsolidatedData)
            .Returns(mockAggregatedData);

        // ACT
        var result = await _sut.TransferAsync(jobId, sui);

        // ASSERT
        result.Should().BeSameAs(mockAggregatedData);

        await _mockRecordFinder.Received().FindRecordsAsync(sui, Arg.Any<CancellationToken>());
        await _mockRecordFetcher
            .Received()
            .FetchRecordsAsync(sui, mockRecordPointers, Arg.Any<CancellationToken>());
        _mockRecordConsolidator.Received().ConsolidateRecords(mockUnconsolidatedData);
        _mockConsolidatedDataAggregator.Received().ApplyAggregations(jobId, mockConsolidatedData);
        await _mockAggregatedDataRepository.Received().AddOrUpdateAsync(mockAggregatedData);
    }
}
