using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using SUI.Find.Application.Enums;
using SUI.Find.FindApi.Functions.HttpTriggers;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchStatusFunctionTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private static readonly ILogger<SearchStatusFunction> Logger = Substitute.For<
        ILogger<SearchStatusFunction>
    >();
    private readonly SearchStatusFunction _sut = Substitute.ForPartsOf<SearchStatusFunction>(
        Logger
    );

    [Theory]
    [InlineData(OrchestrationRuntimeStatus.Pending, SearchStatus.Queued)]
    [InlineData(OrchestrationRuntimeStatus.Running, SearchStatus.Running)]
    [InlineData(OrchestrationRuntimeStatus.Completed, SearchStatus.Completed)]
    [InlineData(OrchestrationRuntimeStatus.Terminated, SearchStatus.Cancelled)]
    [InlineData(OrchestrationRuntimeStatus.Failed, SearchStatus.Failed)]
    public async Task ConvertOrchestrationStatusToSearchStatus_ReturnsCorrectStatus(
        OrchestrationRuntimeStatus orchestrationStatus,
        SearchStatus expectedSearchStatus
    )
    {
        // Arrange
        var queryData = new Dictionary<string, StringValues>();
        var httpRequestMock = Mocks.MockHttpRequestData.Create(queryData);
        var mockedOrchestrationMetadata = new OrchestrationMetadata(
            "SearchOrchestrator",
            "test-job-id"
        )
        {
            DataConverter = null,
            RuntimeStatus = orchestrationStatus,
            CreatedAt = default,
            LastUpdatedAt = default,
            SerializedInput = "test-suid",
        };
        _client
            .GetInstanceAsync(
                "test-job-id",
                getInputsAndOutputs: true,
                Arg.Any<CancellationToken>()
            )
            .Returns(mockedOrchestrationMetadata);

        // Have to sub as the ReadInputAs on the orchestration metadata is sealed and cannot be mocked directly
        _sut.GetSuidFromJobStatus(Arg.Any<OrchestrationMetadata>()).Returns("test-suid");

        // Act
        var result = await _sut.SearchJobTrigger(
            httpRequestMock,
            _client,
            _context,
            "test-job-id",
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var searchJob = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.Equal(expectedSearchStatus, searchJob!.Status);
    }

    [Fact]
    public async Task ShouldReturnAllSearchJobProperties_WhenJobIsFound()
    {
        // Arrange
        var queryData = new Dictionary<string, StringValues>();
        var httpRequestMock = Mocks.MockHttpRequestData.Create(queryData);
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0);
        var lastUpdatedAt = new DateTime(2024, 1, 1, 12, 30, 0);
        var mockedOrchestrationMetadata = new OrchestrationMetadata(
            "SearchOrchestrator",
            "test-job-id"
        )
        {
            DataConverter = null,
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
            CreatedAt = createdAt,
            LastUpdatedAt = lastUpdatedAt,
            SerializedInput = "test-suid",
        };
        _client
            .GetInstanceAsync(
                "test-job-id",
                getInputsAndOutputs: true,
                Arg.Any<CancellationToken>()
            )
            .Returns(mockedOrchestrationMetadata);

        // Have to sub as the ReadInputAs on the orchestration metadata is sealed and cannot be mocked directly
        _sut.GetSuidFromJobStatus(Arg.Any<OrchestrationMetadata>()).Returns("test-suid");

        // Act
        var result = await _sut.SearchJobTrigger(
            httpRequestMock,
            _client,
            _context,
            "test-job-id",
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var searchJob = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.NotNull(searchJob);
        Assert.Equal("test-job-id", searchJob.JobId);
        Assert.Equal("test-suid", searchJob.Suid);
        Assert.Equal(SearchStatus.Running, searchJob.Status);
        Assert.True(EqualDates(createdAt, searchJob.CreatedAt));
        Assert.True(EqualDates(lastUpdatedAt, searchJob.LastUpdatedAt));
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenJobStatusReturnsNull()
    {
        // Arrange
        var queryData = new Dictionary<string, StringValues>();
        var httpRequestMock = Mocks.MockHttpRequestData.Create(queryData);
        _client
            .GetInstanceAsync(
                "non-existent-job-id",
                getInputsAndOutputs: true,
                Arg.Any<CancellationToken>()
            )
            .Returns((OrchestrationMetadata?)null);

        // Act
        var result = await _sut.SearchJobTrigger(
            httpRequestMock,
            _client,
            _context,
            "non-existent-job-id",
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var searchJob = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(searchJob);
        Assert.Equal(404, searchJob.Status);
    }

    private static bool EqualDates(DateTimeOffset expected, DateTimeOffset actual)
    {
        return expected.ToUniversalTime() == actual.ToUniversalTime();
    }
}
