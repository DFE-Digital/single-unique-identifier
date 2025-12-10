using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using SUI.Transfer.API.Endpoint;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.API.Unit.Tests.Endpoint;

public class TransferEndpointTests
{
    private readonly ITransferService _mockTransferService = Substitute.For<ITransferService>();

    [Fact]
    public void Transfer_ReturnsQueuedJob()
    {
        //Arrange
        var testId = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new QueuedTransferJobState(Guid.NewGuid(), testId, createdAt);

        _mockTransferService.BeginTransferJob(Arg.Any<string>()).Returns(mockResponse);

        // Act
        var result = TransferEndpoint.Transfer(testId, _mockTransferService);

        // Assert
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Equal(
            new QueuedTransferJobState(mockResponse.JobId, testId, createdAt)
            {
                LastUpdatedAt = result.Value.LastUpdatedAt,
            },
            result.Value
        );
    }

    [Fact]
    public void CancelTransferJob_ThrowsNotImplementedException()
    {
        var testId = Guid.Empty;

        Assert.Throws<NotImplementedException>(() => TransferEndpoint.CancelTransferJob(testId));
    }

    [Fact]
    public async Task GetTransferJobStatus_ReturnsQueuedJobStatus()
    {
        //Arrange
        var sui = "999-000-1234";
        var testId = Guid.NewGuid();
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new RunningTransferJobState(testId, sui, createdAt, createdAt);

        _mockTransferService.GetTransferJobStateAsync(Arg.Is(testId)).Returns(mockResponse);

        // Act
        var result = await TransferEndpoint.GetTransferJobStatus(testId, _mockTransferService);

        // Assert
        Assert.NotNull(result.Result);
        var okResult = result.Result as Ok<TransferJobState>;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(mockResponse, okResult.Value);
    }

    [Fact]
    public async Task GetTransferResult_ReturnsCompletedJob()
    {
        //Arrange
        var sui = "999-000-1234";
        var testId = Guid.NewGuid();
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new CompletedTransferJobState(
            testId,
            sui,
            new ConformedData(
                testId,
                new ConsolidatedData(sui)
                {
                    PersonalDetailsRecord = null,
                    ChildrensServicesDetailsRecord = null,
                    EducationDetailsRecord = null,
                    HealthDataRecord = null,
                    CrimeDataRecord = null,
                    CountOfRecordsSuccessfullyFetched = 0,
                    FailedFetches = [],
                },
                createdAt
            )
            {
                EducationAttendanceSummaries = null,
                HealthAttendanceSummaries = null,
                ChildServicesReferralSummaries = null,
                CrimeMissingEpisodesSummaries = null,
                StatusFlags = null,
            },
            createdAt,
            createdAt
        );

        _mockTransferService.GetTransferJobStateAsync(Arg.Is(testId)).Returns(mockResponse);

        // Act
        var result = await TransferEndpoint.GetTransferResult(testId, _mockTransferService);

        // Assert
        Assert.NotNull(result.Result);
        var okResult = result.Result as Ok<CompletedTransferJobState>;
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(mockResponse, okResult.Value);
    }
}
