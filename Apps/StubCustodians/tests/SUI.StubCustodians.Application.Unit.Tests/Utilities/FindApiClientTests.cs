using System.Net;
using System.Text;
using System.Text.Json;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Utilities;

namespace SUI.StubCustodians.Application.Unit.Tests.Utilities;

public class FindApiClientTests
{
    [Fact]
    public async Task ClaimAsync_ShouldReturnJob_WhenJobAvailable()
    {
        var job = new JobInfo
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            Sui = "person-123",
            RecordType = "education.details",
            CustodianId = "custodian-1",
            LeaseExpiresAtUtc = default,
        };

        var handler = new FakeHandler(req =>
        {
            Assert.Equal("/v2/work/claim", req.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
            Assert.Equal("token", req.Headers.Authorization.Parameter);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(job),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var api = new FindApiClient(client);

        var result = await api.ClaimAsync("token");

        Assert.NotNull(result);
        Assert.Equal("job-1", result!.JobId);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ClaimAsync_ShouldReturnNull_WhenNoWorkAvailable()
    {
        var handler = new FakeHandler(req => new HttpResponseMessage(HttpStatusCode.NoContent));

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var api = new FindApiClient(client);

        var result = await api.ClaimAsync("token");

        Assert.Null(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ClaimAsync_ShouldThrow_WhenHttpFails()
    {
        var handler = new FakeHandler(req => new HttpResponseMessage(
            HttpStatusCode.InternalServerError
        ));

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var api = new FindApiClient(client);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.ClaimAsync("token"));
    }

    [Fact]
    public async Task SubmitAsync_ShouldPostResults()
    {
        var handler = new FakeHandler(req =>
        {
            Assert.Equal("/v2/work/result", req.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
            Assert.Equal("token", req.Headers.Authorization.Parameter);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var api = new FindApiClient(client);

        var request = new SubmitJobResultsRequest
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            ResultType = "HasRecords",
            Records = [],
        };

        await api.SubmitAsync("token", request);

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SubmitAsync_ShouldIgnoreConflict()
    {
        var handler = new FakeHandler(req => new HttpResponseMessage(HttpStatusCode.Conflict));

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var api = new FindApiClient(client);

        var request = new SubmitJobResultsRequest
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            ResultType = "NoRecords",
            Records = [],
        };

        await api.SubmitAsync("token", request);

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SubmitAsync_ShouldThrow_WhenHttpFails()
    {
        var handler = new FakeHandler(req => new HttpResponseMessage(HttpStatusCode.BadRequest));

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var api = new FindApiClient(client);

        var request = new SubmitJobResultsRequest
        {
            JobId = "job-1",
            LeaseId = "lease-1",
            ResultType = "HasRecords",
            Records = [],
        };

        await Assert.ThrowsAsync<HttpRequestException>(() => api.SubmitAsync("token", request));
    }
}
