using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class RenewLeaseFunctionTests
{
    private readonly ILogger<RenewLeaseFunction> _mockLogger = Substitute.For<
        ILogger<RenewLeaseFunction>
    >();
    private readonly IJobClaimService _mockJobClaimService = Substitute.For<IJobClaimService>();

    private readonly RenewLeaseFunction _sut;

    public RenewLeaseFunctionTests()
    {
        _sut = new RenewLeaseFunction(_mockLogger, _mockJobClaimService);
    }

    [Fact]
    public async Task RenewLease_ShouldReturn_Unauthorized_WhenAuthContextIsMissing()
    {
        // ARRANGE
        var requestBody = new RenewJobLeaseRequest { JobId = "job-id", LeaseId = "lease-id" };
        var request = CreateHttpRequestData(requestBody);
        var context = Substitute.For<FunctionContext>();

        // ACT
        var result = await _sut.RenewLease(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RenewLease_ShouldReturn_BadRequest_WhenRequestBodyIsInvalid()
    {
        // ARRANGE
        var request = MockHttpRequestData.Create(); // Empty body
        var context = CreateContextWithAuth("test-client-id");

        // ACT
        var result = await _sut.RenewLease(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenewLease_ShouldReturn_Ok_WhenLeaseIsRenewedSuccessfully()
    {
        // ARRANGE
        const string testClientId = "test-client-id";
        var requestBody = new RenewJobLeaseRequest { JobId = "job123", LeaseId = "lease456" };
        var request = CreateHttpRequestData(requestBody);
        var context = CreateContextWithAuth(testClientId);

        var expectedJobInfo = new JobInfo
        {
            JobId = requestBody.JobId,
            LeaseId = requestBody.LeaseId,
            CustodianId = testClientId,
            WorkItemId = "workItem789",
            LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(30),
        };

        _mockJobClaimService
            .ExtendJobLeaseAsync(
                testClientId,
                requestBody.JobId,
                requestBody.LeaseId,
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult<JobInfo?>(expectedJobInfo));

        // ACT
        var result = await _sut.RenewLease(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        await _mockJobClaimService
            .Received(1)
            .ExtendJobLeaseAsync(
                testClientId,
                requestBody.JobId,
                requestBody.LeaseId,
                Arg.Any<CancellationToken>()
            );

        result.Body.Position = 0;
        var responseBody = JsonSerializer.Deserialize<RenewJobLeaseResponse>(
            result.Body,
            JsonSerializerOptions.Web
        );
        responseBody.ShouldNotBeNull();
        responseBody.JobId.ShouldBe(expectedJobInfo.JobId);
        responseBody.LeaseId.ShouldBe(expectedJobInfo.LeaseId);
        responseBody.WorkItemId.ShouldBe(expectedJobInfo.WorkItemId);
        responseBody.LeaseExpiresUtc.ShouldBe(expectedJobInfo.LeaseExpiresAtUtc);
    }

    [Fact]
    public async Task RenewLease_ShouldReturn_NoContent_WhenJobInfoIsNull()
    {
        // ARRANGE
        const string testClientId = "test-client-id";
        var requestBody = new RenewJobLeaseRequest { JobId = "job123", LeaseId = "lease456" };
        var request = CreateHttpRequestData(requestBody);
        var context = CreateContextWithAuth(testClientId);

        _mockJobClaimService
            .ExtendJobLeaseAsync(
                testClientId,
                requestBody.JobId,
                requestBody.LeaseId,
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult<JobInfo?>(null));

        // ACT
        var result = await _sut.RenewLease(request, context, CancellationToken.None);

        // ASSERT
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _mockJobClaimService
            .Received(1)
            .ExtendJobLeaseAsync(
                testClientId,
                requestBody.JobId,
                requestBody.LeaseId,
                Arg.Any<CancellationToken>()
            );
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

    private static HttpRequestData CreateHttpRequestData(RenewJobLeaseRequest? requestBody)
    {
        return requestBody == null
            ? MockHttpRequestData.Create()
            : MockHttpRequestData.CreateJson(requestBody);
    }
}
