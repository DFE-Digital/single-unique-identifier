using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class ClaimJobFunctionTests
{
    private readonly ILogger<ClaimJobFunction> _mockLogger = Substitute.For<
        ILogger<ClaimJobFunction>
    >();
    private readonly IJobClaimService _mockJobClaimService = Substitute.For<IJobClaimService>();

    private readonly ClaimJobFunction _sut;

    public ClaimJobFunctionTests()
    {
        _sut = new ClaimJobFunction(_mockLogger, _mockJobClaimService);
    }

    [Fact]
    public async Task ClaimJob_ShouldReturn_Unauthorized_WhenAuthContextIsMissing()
    {
        var request = MockHttpRequestData.Create();
        var context = Substitute.For<FunctionContext>();

        // ACT
        var result = await _sut.ClaimJob(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClaimJob_ShouldReturn_Created_WhenJobIsAvailableAndClaimed()
    {
        const string testOrgId = "example-submitting-custodian-id";

        var request = MockHttpRequestData.Create();
        var context = CreateContextWithAuth(testOrgId);

        var exampleClaimedJob = new JobInfo
        {
            JobId = $"job-{Guid.NewGuid()}",
            CustodianId = testOrgId,
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            LeaseId = $"lease-{Guid.NewGuid()}",
            WorkItemId = $"wi-{Guid.NewGuid()}",
            Sui = $"sui-{Guid.NewGuid()}",
            RecordType = "education.details",
        };

        _mockJobClaimService
            .ClaimNextAvailableJobAsync(testOrgId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<JobInfo?>(exampleClaimedJob));

        // ACT
        var result = await _sut.ClaimJob(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.Created);

        await _mockJobClaimService
            .Received(1)
            .ClaimNextAvailableJobAsync(testOrgId, Arg.Any<CancellationToken>());

        // Assert the response body
        result.Body.Position = 0;
        var claimedJob = JsonSerializer.Deserialize<JobInfo>(result.Body);
        claimedJob.ShouldBeEquivalentTo(exampleClaimedJob);
    }

    [Fact]
    public async Task ClaimJob_ShouldReturn_NoContent_WhenNoJobsAreAvailable()
    {
        const string testOrgId = "example-submitting-custodian-id";

        var request = MockHttpRequestData.Create();
        var context = CreateContextWithAuth(testOrgId);

        _mockJobClaimService
            .ClaimNextAvailableJobAsync(testOrgId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<JobInfo?>(null));

        // ACT
        var result = await _sut.ClaimJob(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await _mockJobClaimService
            .Received(1)
            .ClaimNextAvailableJobAsync(testOrgId, Arg.Any<CancellationToken>());

        // Assert the response body
        result.Body.Length.ShouldBe(0);
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
