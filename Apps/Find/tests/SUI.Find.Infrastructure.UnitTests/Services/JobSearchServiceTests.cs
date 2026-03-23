using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

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
        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                Arg.Any<string>(),
                JobType.CustodianLookup,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(null as WorkItemJobCount);

        var result = await _sut.GetSearchResultsAsync("WID-1", "SOID-1", CancellationToken.None);

        Assert.IsType<NotFound>(result.Value);
        _logger.ReceivedWithAnyArgs(1).LogInformation("No jobs found for work item ID WID-1");
    }

    [Fact]
    public async Task GetSearchResults_ReturnsNotFound_WhenExpectedJobCountIsEmpty()
    {
        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                Arg.Any<string>(),
                JobType.CustodianLookup,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new WorkItemJobCount
                {
                    WorkItemId = "WID-1",
                    JobType = JobType.CustodianLookup,
                    SearchingOrganisationId = "SOID-1",
                    PayloadJson = string.Empty,
                    ExpectedJobCount = 0,
                }
            );

        var result = await _sut.GetSearchResultsAsync("WID-1", "SOID-1", CancellationToken.None);

        Assert.IsType<NotFound>(result.Value);
        _logger.ReceivedWithAnyArgs(1).LogInformation("No jobs found for work item ID WID-1");
    }

    [Fact]
    public async Task GetSearchResults_CorrectlyReturnsCompletedResults()
    {
        var workItemId = "WID-1";
        var searchingOrganisationId = "SOID-1";

        var payload = new CustodianLookupJobPayload("SUI-1", "Health");

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 3,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                searchingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

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
                SearchingOrganisationId = searchingOrganisationId,
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
                SearchingOrganisationId = searchingOrganisationId,
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
                SearchingOrganisationId = searchingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(workItemId, searchingOrganisationId, Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            searchingOrganisationId,
            CancellationToken.None
        );

        Assert.IsType<SearchResultsV2Dto>(results.Value);
        Assert.Equal(100, results.AsT0.CompletenessPercentage);
        Assert.Equal(SearchStatus.Completed, results.AsT0.Status);
        Assert.Equal(payload.Sui, results.AsT0.Suid);
        Assert.Equal(workItemId, results.AsT0.WorkItemId);
        Assert.Equal(searchResults, results.AsT0.Items);
    }

    [Fact]
    public async Task GetSearchResults_CorrectlyReturnsExpiredResults()
    {
        var workItemId = "WID-1";
        var searchingOrganisationId = "SOID-1";

        var payload = new CustodianLookupJobPayload("SUI-1", "HEALTH") { Sui = "SUI-1" };

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 5,
            CreatedAtUtc = _dateTime.AddDays(-6),
            UpdatedAtUtc = _dateTime.AddHours(-5),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                searchingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

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
                SearchingOrganisationId = searchingOrganisationId,
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
                SearchingOrganisationId = searchingOrganisationId,
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
                SearchingOrganisationId = searchingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(workItemId, searchingOrganisationId, Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            searchingOrganisationId,
            CancellationToken.None
        );

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
        var searchingOrganisationId = "SOID-1";
        var payload = new CustodianLookupJobPayload("SUI-1", "HEALTH");

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 5,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                searchingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

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
                SearchingOrganisationId = searchingOrganisationId,
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
                SearchingOrganisationId = searchingOrganisationId,
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
                SearchingOrganisationId = searchingOrganisationId,
            },
        ];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(workItemId, searchingOrganisationId, Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            searchingOrganisationId,
            CancellationToken.None
        );

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
        var searchingOrganisationId = "SOID-1";
        var payload = new CustodianLookupJobPayload("SUI-1", "HEALTH");

        var workItemJobCount = new WorkItemJobCount
        {
            WorkItemId = workItemId,
            JobType = JobType.CustodianLookup,
            SearchingOrganisationId = searchingOrganisationId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            ExpectedJobCount = 5,
            CreatedAtUtc = _dateTime.AddHours(-6),
            UpdatedAtUtc = _dateTime.AddHours(-2),
        };

        _workItemJobCountRepository
            .GetByWorkItemIdAndJobTypeAsync(
                workItemId,
                JobType.CustodianLookup,
                searchingOrganisationId,
                Arg.Any<CancellationToken>()
            )
            .Returns(workItemJobCount);

        IReadOnlyList<SearchResultEntry> searchResults = [];

        _searchResultEntryRepository
            .GetByWorkItemIdAsync(workItemId, searchingOrganisationId, Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var results = await _sut.GetSearchResultsAsync(
            workItemId,
            searchingOrganisationId,
            CancellationToken.None
        );

        Assert.IsType<SearchResultsV2Dto>(results.Value);
        Assert.Equal(0, results.AsT0.CompletenessPercentage);
        Assert.Equal(SearchStatus.Running, results.AsT0.Status);
        Assert.Equal(payload.Sui, results.AsT0.Suid);
        Assert.Equal(workItemId, results.AsT0.WorkItemId);
        Assert.Equal(searchResults, results.AsT0.Items);
    }
}
