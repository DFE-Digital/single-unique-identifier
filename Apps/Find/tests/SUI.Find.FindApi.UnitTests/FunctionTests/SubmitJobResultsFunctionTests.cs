using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.JobRepository;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SubmitJobResultsFunctionTests
{
    private readonly ILogger<SubmitJobResultsFunction> _logger = Substitute.For<
        ILogger<SubmitJobResultsFunction>
    >();
    private readonly IJobProcessorService _jobService = Substitute.For<IJobProcessorService>();
    private readonly IJobResultsQueueClient _queueClient = Substitute.For<IJobResultsQueueClient>();

    private readonly SubmitJobResultsFunction _function;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public SubmitJobResultsFunctionTests()
    {
        _function = new SubmitJobResultsFunction(_logger, _jobService, _queueClient);
    }

    [Fact]
    public async Task SubmitJobResults_ShouldReturnUnauthorized_WhenAuthMissing()
    {
        var request = MockHttpRequestData.CreateJson(CreateValidRequest());
        var context = Substitute.For<FunctionContext>();

        var result = await _function.SubmitJobResults(request, context, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task SubmitJobResults_ShouldReturnBadRequest_WhenPayloadInvalid()
    {
        var request = MockHttpRequestData.CreateJson(""); // malformed JSON
        var context = CreateContextWithAuth("cust-1");

        var result = await _function.SubmitJobResults(request, context, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task SubmitJobResults_ShouldReturnBadRequest_WhenLeaseInvalid()
    {
        var requestData = CreateValidRequest();
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("cust-1");

        _jobService
            .ValidateLeaseAsync(
                requestData.JobId,
                requestData.LeaseId,
                "cust-1",
                Arg.Any<CancellationToken>()
            )
            .Returns((Job?)null);

        var result = await _function.SubmitJobResults(request, context, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task SubmitJobResults_ShouldReturnAccepted_AndSendQueueMessage_WhenValid()
    {
        var requestData = CreateValidRequest();
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("cust-1");

        var job = new Job
        {
            JobId = requestData.JobId,
            SearchingOrganisationId = "searching-org-1",
            CustodianId = "cust-1",
            JobType = JobType.CustodianLookup,
            WorkItemId = "work-123",
            LeaseId = requestData.LeaseId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = null,
            PayloadJson = "{}",
        };

        _jobService
            .ValidateLeaseAsync(
                requestData.JobId,
                requestData.LeaseId,
                "cust-1",
                Arg.Any<CancellationToken>()
            )
            .Returns(job);

        var result = await _function.SubmitJobResults(request, context, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);

        await _queueClient
            .Received(1)
            .SendAsync(
                Arg.Is<JobResultMessage>(m =>
                    m.JobId == job.JobId
                    && m.WorkItemId == job.WorkItemId
                    && m.CustodianId == "cust-1"
                    && m.LeaseId == requestData.LeaseId
                    && m.Records.Count == requestData.Records.Count
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SubmitJobResults_ShouldReturnConflict_WhenJobAlreadyCompleted()
    {
        var requestData = CreateValidRequest();
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("cust-1");

        var job = new Job
        {
            JobId = requestData.JobId,
            SearchingOrganisationId = "searching-org-1",
            CustodianId = "cust-1",
            JobType = JobType.CustodianLookup,
            WorkItemId = "work-123",
            LeaseId = requestData.LeaseId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow, // key condition
            PayloadJson = "{}",
        };

        _jobService
            .ValidateLeaseAsync(
                requestData.JobId,
                requestData.LeaseId,
                "cust-1",
                Arg.Any<CancellationToken>()
            )
            .Returns(job);

        var result = await _function.SubmitJobResults(request, context, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);

        await _queueClient
            .DidNotReceive()
            .SendAsync(Arg.Any<JobResultMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitJobResults_ShouldAddNoCacheHeaders_WhenAccepted()
    {
        var requestData = CreateValidRequest();
        var request = MockHttpRequestData.CreateJson(requestData);
        var context = CreateContextWithAuth("cust-1");

        var job = new Job
        {
            JobId = requestData.JobId,
            SearchingOrganisationId = "searching-org-1",
            CustodianId = "cust-1",
            JobType = JobType.CustodianLookup,
            WorkItemId = "work-123",
            LeaseId = requestData.LeaseId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = null,
            PayloadJson = "{}",
        };

        _jobService
            .ValidateLeaseAsync(
                requestData.JobId,
                requestData.LeaseId,
                "cust-1",
                Arg.Any<CancellationToken>()
            )
            .Returns(job);

        var result = await _function.SubmitJobResults(request, context, CancellationToken.None);

        var cacheControl = result.Headers.GetValues("Cache-Control").First();

        Assert.Contains("no-store", cacheControl);
        Assert.Contains("no-cache", cacheControl);
        Assert.Contains("must-revalidate", cacheControl);
        Assert.Contains("max-age=0", cacheControl);

        Assert.Equal("no-cache", result.Headers.GetValues("Pragma").First());

        Assert.Equal(
            DateTimeOffset.UnixEpoch.ToString("R"),
            result.Headers.GetValues("Expires").First()
        );

        Assert.Equal("Authorization", result.Headers.GetValues("Vary").First());
    }

    // Helpers
    private static SubmitJobResultsRequest CreateValidRequest()
    {
        return new SubmitJobResultsRequest
        {
            JobId = "job-123",
            LeaseId = "lease-123",
            ResultType = "HasRecords",
            Records =
            [
                new JobResultRecord
                {
                    SystemId = "sys-1",
                    RecordType = "TYPE",
                    RecordUrl = "http://test",
                    RecordId = "rec-1",
                },
            ],
        };
    }

    private static FunctionContext CreateContextWithAuth(string clientId)
    {
        var context = Substitute.For<FunctionContext>();
        var authContext = new AuthContext(clientId, ["work-item.write"]);

        var items = new Dictionary<object, object>
        {
            { ApplicationConstants.Auth.AuthContextKey, authContext },
        };

        context.Items.Returns(items);
        context.TraceContext.Returns(Substitute.For<TraceContext>());
        context.InvocationId.Returns("test-invocation-id");

        return context;
    }
}
