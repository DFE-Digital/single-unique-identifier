using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class TransferJobTests
{
    private readonly IRecordFinder _mockRecordFinder = Substitute.For<IRecordFinder>();
    private readonly IRecordFetcher _mockRecordFetcher = Substitute.For<IRecordFetcher>();
    private readonly IRecordConsolidator _mockRecordConsolidator =
        Substitute.For<IRecordConsolidator>();
    private readonly IEducationAttendanceTransformer _mockEducationAttendanceTransformer =
        Substitute.For<IEducationAttendanceTransformer>();
    private readonly IHealthAttendanceAggregator _mockHealthAttendanceAggregator =
        Substitute.For<IHealthAttendanceAggregator>();
    private readonly IChildServicesReferralAggregator _mockChildServicesReferralAggregator =
        Substitute.For<IChildServicesReferralAggregator>();
    private readonly IMissingEpisodesTransformer _mockMissingEpisodesTransformer =
        Substitute.For<IMissingEpisodesTransformer>();
    private readonly IStatusFlagsTransformer _mockStatusFlagsTransformer =
        Substitute.For<IStatusFlagsTransformer>();
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
            _mockEducationAttendanceTransformer,
            _mockHealthAttendanceAggregator,
            _mockChildServicesReferralAggregator,
            _mockMissingEpisodesTransformer,
            _mockStatusFlagsTransformer,
            _mockHostApplicationLifetime,
            _mockLogger,
            TimeProvider.System
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
            PersonalDetailsRecords = [],
            ChildrensServicesDetailsRecords = [],
            EducationDetailsRecords = [],
            HealthDataRecords = [],
            CrimeDataRecords = [],
            FailedFetches = [],
        };

        _mockRecordFetcher
            .FetchRecordsAsync(sui, mockRecordPointers, Arg.Any<CancellationToken>())
            .Returns(mockUnconsolidatedData);

        ConsolidatedData mockConsolidatedData = new(sui)
        {
            PersonalDetailsRecord = null,
            ChildrensServicesDetailsRecord = null,
            EducationDetailsRecord = null,
            HealthDataRecord = null,
            CrimeDataRecord = null,
            CountOfRecordsSuccessfullyFetched = 0,
            FailedFetches = [],
        };

        _mockRecordConsolidator
            .ConsolidateRecords(mockUnconsolidatedData)
            .Returns(mockConsolidatedData);

        var mockEducationAttendanceSummaries = new EducationAttendanceSummaries
        {
            CurrentAcademicYear = new EducationAttendanceV1 { AttendancePercentage = 90 },
            LastAcademicYear = new EducationAttendanceV1 { AttendancePercentage = 80 },
        };

        _mockEducationAttendanceTransformer
            .ApplyTransformation(mockConsolidatedData)
            .Returns(mockEducationAttendanceSummaries);

        var mockHealthAttendanceSummaries = new HealthAttendanceSummaries
        {
            Last5Years = new HealthAttendanceSummary(1, 2, 3, 4),
            Last12Months = null,
        };

        _mockHealthAttendanceAggregator
            .ApplyAggregation(mockConsolidatedData)
            .Returns(mockHealthAttendanceSummaries);

        var mockChildServicesReferralSummaries = new ChildServicesReferralSummaries
        {
            Past6Months = [],
            Past12Months = null,
            Past5Years = null,
        };

        _mockChildServicesReferralAggregator
            .ApplyAggregation(mockConsolidatedData)
            .Returns(mockChildServicesReferralSummaries);

        var mockCrimeMissingEpisodesSummaries = new CrimeMissingEpisodesSummaries
        {
            Last6Months = [],
        };

        _mockMissingEpisodesTransformer
            .ApplyTransformation(mockConsolidatedData)
            .Returns(mockCrimeMissingEpisodesSummaries);

        var mockStatusFlags = new StatusFlag[8];

        _mockStatusFlagsTransformer
            .ApplyTransformation(mockConsolidatedData)
            .Returns(mockStatusFlags);

        ConformedData expectedConformedData = new(
            jobId,
            mockConsolidatedData,
            TimeProvider.System.GetUtcNow()
        )
        {
            EducationAttendanceSummaries = mockEducationAttendanceSummaries,
            HealthAttendanceSummaries = mockHealthAttendanceSummaries,
            ChildServicesReferralSummaries = mockChildServicesReferralSummaries,
            CrimeMissingEpisodesSummaries = mockCrimeMissingEpisodesSummaries,
            StatusFlags = mockStatusFlags,
        };

        // ACT
        var result = await _sut.TransferAsync(jobId, sui);

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                expectedConformedData,
                options => options.Excluding(x => x.CreatedDate)
            );

        await _mockRecordFinder.Received().FindRecordsAsync(sui, Arg.Any<CancellationToken>());
        await _mockRecordFetcher
            .Received()
            .FetchRecordsAsync(sui, mockRecordPointers, Arg.Any<CancellationToken>());
        _mockRecordConsolidator.Received().ConsolidateRecords(mockUnconsolidatedData);
        _mockEducationAttendanceTransformer.Received().ApplyTransformation(mockConsolidatedData);
        _mockHealthAttendanceAggregator.Received().ApplyAggregation(mockConsolidatedData);
        _mockChildServicesReferralAggregator.Received().ApplyAggregation(mockConsolidatedData);
        _mockMissingEpisodesTransformer.Received().ApplyTransformation(mockConsolidatedData);
        _mockStatusFlagsTransformer.Received().ApplyTransformation(mockConsolidatedData);
    }
}
