using System.Collections.Frozen;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using Shouldly;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

// Ignore "Evaluation of this argument may be expensive and unnecessary if logging is disabled" - these are tests!
#pragma warning disable CA1873

public class JobSearchServiceTests
{
    private readonly IWorkItemJobCountRepository _workItemJobCountRepository =
        Substitute.For<IWorkItemJobCountRepository>();
    private readonly ISearchResultEntryRepository _searchResultEntryRepository =
        Substitute.For<ISearchResultEntryRepository>();
    private readonly ILogger<JobSearchService> _logger = Substitute.For<
        ILogger<JobSearchService>
    >();
    private readonly JobSearchService _sut;
    private readonly DateTime _dateTime = new(2026, 03, 01);
    private readonly IJobWindowStartService _jobWindowStartService =
        Substitute.For<IJobWindowStartService>();

    public JobSearchServiceTests()
    {
        _logger.IsEnabled(LogLevel.Information).Returns(true);

        _jobWindowStartService.GetWindowStart().Returns(_dateTime.AddHours(-72));

        _sut = new JobSearchService(
            _searchResultEntryRepository,
            _workItemJobCountRepository,
            _jobWindowStartService,
            _logger
        );
    }

    [Fact]
    public async Task GetSearchResults_ReturnsNotFound_WhenNoWorkItemJobCountExists()
    {
        var workItemId = "WID-1";
        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(null as WorkItemJobCount);

        // ACT
        var result = await _sut.GetSearchResultsAsync(workItemId, "ROID-1", CancellationToken.None);

        // ASSERT
        Assert.IsType<NotFound>(result.Value);
        _logger
            .Received(1)
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>((object x) => $"{x}" == "No jobs found for work item ID WID-1"),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task GetSearchResults_ReturnsNotFound_WhenExpectedJobCountIsEmpty()
    {
        var workItemId = "WID-1";
        var requestingOrganisationId = "ROID-1";

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new WorkItemJobCount
                {
                    WorkItemId = workItemId,
                    JobType = JobType.CustodianLookup,
                    RequestingOrganisationId = requestingOrganisationId,
                    PayloadJson = string.Empty,
                    ExpectedJobCount = 0,
                }
            );

        // ACT
        var result = await _sut.GetSearchResultsAsync(
            workItemId,
            requestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        Assert.IsType<NotFound>(result.Value);
        _logger
            .Received(1)
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>((object x) => $"{x}" == "No jobs found for work item ID WID-1"),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task GetSearchResults_ReturnsForbidden_WhenRequestingOrganisationIdIsWrong()
    {
        var workItemId = "WID-1";
        var requestingOrganisationId = "ROID-1";

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new WorkItemJobCount
                {
                    WorkItemId = workItemId,
                    JobType = JobType.CustodianLookup,
                    RequestingOrganisationId = requestingOrganisationId,
                    PayloadJson = string.Empty,
                    ExpectedJobCount = 3,
                }
            );

        var differentRequestingOrganisationId = "ROID-2";

        // ACT
        var result = await _sut.GetSearchResultsAsync(
            workItemId,
            differentRequestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        Assert.IsType<Forbidden>(result.Value);
        _logger
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<Arg.AnyType>(
                    (object x) =>
                        $"{x}"
                        == "Requesting organisation ID (ROID-2) from request does not match organisation ID (ROID-1) on work item. Work item ID: WID-1"
                ),
                null,
                Arg.Any<Func<Arg.AnyType, Exception?, string>>()
            );
    }

    [Fact]
    public async Task GetSearchResults_CorrectlyReturnsCompletedResults()
    {
        var workItemId = "WID-1";
        var requestingOrganisationId = "ROID-1";

        var payload = new CustodianLookupJobPayload("SUI-1", "Health");

        IReadOnlyList<SearchResultEntry> searchResults =
        [
            new()
            {
                CustodianId = "CUS-1",
                SystemId = "SYSID-1",
                CustodianName = "Custodian-1",
                RecordType = "HEALTH",
                RecordUrl = "URL-1",
                JobId = "JOB-1",
                WorkItemId = workItemId,
                RecordId = "12345",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-2",
                SystemId = "SYSID-2",
                CustodianName = "Custodian-2",
                RecordType = "POLICE",
                RecordUrl = "URL-2",
                JobId = "JOB-2",
                WorkItemId = workItemId,
                RecordId = "56789",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-3",
                SystemId = "SYSID-3",
                CustodianName = "Custodian-3",
                RecordType = "EDUCATION",
                RecordUrl = "URL-3",
                JobId = "JOB-3",
                WorkItemId = workItemId,
                RecordId = "00000",
                RequestingOrganisationId = requestingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(
                workItemId,
                requestingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(searchResults);

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            RequestingOrganisationId = requestingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 3,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
            CompletedJobIds = searchResults.Select(x => x.JobId).ToFrozenSet(),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

        // ACT
        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            requestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        Assert.IsType<SearchResultsV2Dto>(results.Value);
        Assert.Equal(100, results.AsT0.CompletenessPercentage);
        Assert.Equal(SearchStatus.Completed, results.AsT0.Status);
        Assert.Equal(payload.Sui, results.AsT0.Suid);
        Assert.Equal(workItemId, results.AsT0.WorkItemId);
        Assert.Equal(searchResults, results.AsT0.Items);
    }

    [Fact]
    public async Task GetSearchResults_UsesDistinctJobCount_ToDeriveCompletenessPercentage()
    {
        var workItemId = $"wi-{Guid.NewGuid()}";
        var requestingOrganisationId = $"ro-{Guid.NewGuid()}";

        var payload = new CustodianLookupJobPayload("SUI-1", "Health");

        IReadOnlyList<SearchResultEntry> searchResults =
        [
            new()
            {
                CustodianId = "CUS-1",
                CustodianName = "Custodian-1",
                JobId = "JOB-1",
                WorkItemId = workItemId,
                RecordType = "HEALTH",
                SystemId = "SYS-1",
                RecordUrl = "URL-1",
                RecordId = "12345",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-1",
                CustodianName = "Custodian-1",
                JobId = "JOB-1",
                WorkItemId = workItemId,
                RecordType = "HEALTH",
                SystemId = "SYS-2",
                RecordUrl = "URL-2",
                RecordId = "xyz",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-1",
                CustodianName = "Custodian-1",
                JobId = "JOB-1",
                WorkItemId = workItemId,
                RecordType = "HEALTH",
                SystemId = "SYS-3",
                RecordUrl = "URL-3",
                RecordId = "efg",
                RequestingOrganisationId = requestingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(
                workItemId,
                requestingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(searchResults);

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            RequestingOrganisationId = requestingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 2,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
            CompletedJobIds = searchResults.Select(x => x.JobId).ToFrozenSet(),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

        // ACT
        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            requestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        var resultsDto = Assert.IsType<SearchResultsV2Dto>(results.Value);
        resultsDto.CompletenessPercentage.ShouldBe(50);
        resultsDto.Status.ShouldBe(SearchStatus.Running);
        resultsDto.WorkItemId.ShouldBe(workItemId);
        resultsDto.Suid.ShouldBe("SUI-1");
    }

    [Fact]
    public async Task GetSearchResults_CorrectlyReturnsExpiredResults()
    {
        var workItemId = "WID-1";
        var requestingOrganisationId = "ROID-1";

        var payload = new CustodianLookupJobPayload("SUI-1", "HEALTH") { Sui = "SUI-1" };

        IReadOnlyList<SearchResultEntry> searchResults =
        [
            new()
            {
                CustodianId = "CUS-1",
                SystemId = "SYSID-1",
                CustodianName = "Custodian-1",
                RecordType = "HEALTH",
                RecordUrl = "URL-1",
                JobId = "JOB-1",
                WorkItemId = workItemId,
                RecordId = "12345",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-2",
                SystemId = "SYSID-2",
                CustodianName = "Custodian-2",
                RecordType = "POLICE",
                RecordUrl = "URL-2",
                JobId = "JOB-2",
                WorkItemId = workItemId,
                RecordId = "56789",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-3",
                SystemId = "SYSID-3",
                CustodianName = "Custodian-3",
                RecordType = "EDUCATION",
                RecordUrl = "URL-3",
                JobId = "JOB-3",
                WorkItemId = workItemId,
                RecordId = "00000",
                RequestingOrganisationId = requestingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(
                workItemId,
                requestingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(searchResults);

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            RequestingOrganisationId = requestingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 5,
            CreatedAtUtc = _dateTime.AddDays(-6),
            UpdatedAtUtc = _dateTime.AddHours(-5),
            CompletedJobIds = searchResults.Select(x => x.JobId).ToFrozenSet(),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

        // ACT
        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            requestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        Assert.IsType<SearchResultsV2Dto>(results.Value);
        Assert.Equal(60, results.AsT0.CompletenessPercentage);
        Assert.Equal(SearchStatus.Expired, results.AsT0.Status);
        Assert.Equal(payload.Sui, results.AsT0.Suid);
        Assert.Equal(workItemId, results.AsT0.WorkItemId);
        Assert.Equal(searchResults, results.AsT0.Items);
    }

    [Fact]
    public async Task GetSearchResults_CorrectlyReturnsPartialResults()
    {
        var workItemId = "WID-1";
        var requestingOrganisationId = "ROID-1";
        var payload = new CustodianLookupJobPayload("SUI-1", "HEALTH");

        IReadOnlyList<SearchResultEntry> searchResults =
        [
            new()
            {
                CustodianId = "CUS-1",
                SystemId = "SYSID-1",
                CustodianName = "Custodian-1",
                RecordType = "HEALTH",
                RecordUrl = "URL-1",
                JobId = "JOB-1",
                WorkItemId = workItemId,
                RecordId = "12345",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-2",
                SystemId = "SYSID-2",
                CustodianName = "Custodian-2",
                RecordType = "POLICE",
                RecordUrl = "URL-2",
                JobId = "JOB-2",
                WorkItemId = workItemId,
                RecordId = "56789",
                RequestingOrganisationId = requestingOrganisationId,
            },
            new()
            {
                CustodianId = "CUS-3",
                SystemId = "SYSID-3",
                CustodianName = "Custodian-3",
                RecordType = "EDUCATION",
                RecordUrl = "URL-3",
                JobId = "JOB-3",
                WorkItemId = workItemId,
                RecordId = "00000",
                RequestingOrganisationId = requestingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(
                workItemId,
                requestingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(searchResults);

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            RequestingOrganisationId = requestingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 5,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
            CompletedJobIds = searchResults.Select(x => x.JobId).ToFrozenSet(),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

        // ACT
        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            requestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        Assert.IsType<SearchResultsV2Dto>(results.Value);
        Assert.Equal(60, results.AsT0.CompletenessPercentage);
        Assert.Equal(SearchStatus.Running, results.AsT0.Status);
        Assert.Equal(payload.Sui, results.AsT0.Suid);
        Assert.Equal(workItemId, results.AsT0.WorkItemId);
        Assert.Equal(searchResults, results.AsT0.Items);
    }

    [Fact]
    public async Task GetSearchResults_CorrectlyReturnsEmptyResults()
    {
        var workItemId = "WID-1";
        var requestingOrganisationId = "ROID-1";
        var payload = new CustodianLookupJobPayload("SUI-1", "HEALTH");

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            RequestingOrganisationId = requestingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 5,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

        IReadOnlyList<SearchResultEntry> searchResults = [];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(
                workItemId,
                requestingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(searchResults);

        // ACT
        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            requestingOrganisationId,
            CancellationToken.None
        );

        // ASSERT
        Assert.IsType<SearchResultsV2Dto>(results.Value);
        Assert.Equal(0, results.AsT0.CompletenessPercentage);
        Assert.Equal(SearchStatus.Running, results.AsT0.Status);
        Assert.Equal(payload.Sui, results.AsT0.Suid);
        Assert.Equal(workItemId, results.AsT0.WorkItemId);
        Assert.Equal(searchResults, results.AsT0.Items);
    }
}
