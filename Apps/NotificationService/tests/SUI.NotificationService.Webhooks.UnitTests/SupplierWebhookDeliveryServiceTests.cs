using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Webhooks.UnitTests;

public class SupplierWebhookDeliveryServiceTests : IDisposable
{
    private readonly TestHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly IWebhookSecretClient _secretClientMock;
    private readonly TimeProvider _timeProviderMock;
    private readonly ILogger<SupplierWebhookDeliveryService> _loggerMock;
    private readonly SupplierWebhookDeliveryService _sut;

    // Track explicitly created HttpResponseMessages to satisfy SonarCloud static analysis
    private readonly List<HttpResponseMessage> _trackedResponses = new();

    public SupplierWebhookDeliveryServiceTests()
    {
        _handler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_handler);

        _secretClientMock = Substitute.For<IWebhookSecretClient>();
        _timeProviderMock = Substitute.For<TimeProvider>();
        _loggerMock = Substitute.For<ILogger<SupplierWebhookDeliveryService>>();

        _sut = new SupplierWebhookDeliveryService(
            _httpClient,
            _secretClientMock,
            _timeProviderMock,
            _loggerMock
        );
    }

    [Fact]
    public async Task DeliverAsync_ProducesExactSignature_FromInteroperabilityTestVector()
    {
        // Arrange - Data directly from the contract document's Interoperability Test Vector
        var deliveryId = Guid.Parse("96e7275d-5fa4-4b1b-b978-21779e03dbd4");
        var eventId = Guid.Parse("27dd8c17-e05d-432f-a14f-6b05d1f7469b");
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(1788341400);
        var base64Secret = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY="; // gitleaks:allow
        var expectedSignature =
            "sha256=703b385e5ad09d1971a40a7a9868ae090d3fb0a77233d0a2fc702785e7c90fa8";

        var request = new WebhookDeliveryRequest(
            SourceEventId: eventId,
            EventType: LifecycleEventType.NhsNumberChanged,
            OccurredAt: DateTimeOffset.Parse("2026-09-02T09:30:00Z"),
            AffectedNhsNumber: "9876543210",
            DeliveryId: deliveryId,
            DeliveryAttempt: 1,
            CorrelationId: Guid.NewGuid(),
            EndpointUrl: "https://supplier.com/webhook",
            KeyId: "key-1",
            SecretKeyVaultReference: "kv-ref"
        );

        _timeProviderMock.GetUtcNow().Returns(timestamp);
        _secretClientMock
            .GetSecretBase64Async(request.SecretKeyVaultReference)
            .Returns(base64Secret);

        _handler.Sender = (req, _) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("https://supplier.com/webhook", req.RequestUri?.ToString());
            Assert.Equal(expectedSignature, req.Headers.GetValues("X-SUI-Signature-256").Single());
            Assert.Equal(
                deliveryId.ToString(),
                req.Headers.GetValues("X-SUI-Delivery-ID").Single()
            );
            Assert.Equal("1788341400", req.Headers.GetValues("X-SUI-Timestamp").Single());
            Assert.Equal("key-1", req.Headers.GetValues("X-SUI-Key-ID").Single());
            Assert.Equal(
                "application/vnd.dfe.sui.lifecycle-notification.v1+json",
                req.Content?.Headers.ContentType?.ToString()
            );

            // Assign, track, and return
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            _trackedResponses.Add(response);
            return Task.FromResult(response);
        };

        // Act
        var result = await _sut.DeliverAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.ResponseReceived);
        Assert.Equal((int)HttpStatusCode.Accepted, result.HttpStatusCode);
    }

    [Fact]
    public async Task DeliverAsync_GeneratesCorrectPayload_ForGpChanged()
    {
        // Arrange
        var request = CreateBaseRequest() with
        {
            EventType = LifecycleEventType.GpChanged,
            AffectedNhsNumber = "9434765919",
        };

        _secretClientMock.GetSecretBase64Async(Arg.Any<string>()).Returns("dGVzdC1zZWNyZXQ=");
        _timeProviderMock.GetUtcNow().Returns(DateTimeOffset.UtcNow);

        _handler.Sender = async (req, _) =>
        {
            var body = await req.Content!.ReadAsStringAsync(_);
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            // Assert required properties exist and are correct
            Assert.Equal("1", root.GetProperty("schemaVersion").GetString());
            Assert.Equal("gpChanged", root.GetProperty("eventType").GetString());
            Assert.Equal("9434765919", root.GetProperty("affectedNhsNumber").GetString());

            // Assert forbidden properties are physically absent
            Assert.Throws<KeyNotFoundException>(() => root.GetProperty("newNhsNumber"));
            Assert.Throws<KeyNotFoundException>(() => root.GetProperty("demographics"));

            // Assign, track, and return
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            _trackedResponses.Add(response);
            return response;
        };

        // Act
        var result = await _sut.DeliverAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.ResponseReceived);
    }

    [Fact]
    public async Task DeliverAsync_ReturnsTimeoutResult_WhenTaskCanceledExceptionThrown()
    {
        // Arrange
        var request = CreateBaseRequest();
        _secretClientMock.GetSecretBase64Async(Arg.Any<string>()).Returns("dGVzdC1zZWNyZXQ=");

        _handler.Sender = (_, _) => throw new TaskCanceledException("Timeout");

        // Act
        var result = await _sut.DeliverAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.ResponseReceived);
        Assert.True(result.TimedOut);
        Assert.False(result.ConnectionFailed);

        // Ensure no PII was logged
        _loggerMock
            .Received(1)
            .Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => !o.ToString()!.Contains(request.AffectedNhsNumber)),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task DeliverAsync_ReturnsConnectionFailedResult_WhenHttpRequestExceptionThrown()
    {
        // Arrange
        var request = CreateBaseRequest();
        _secretClientMock.GetSecretBase64Async(Arg.Any<string>()).Returns("dGVzdC1zZWNyZXQ=");

        _handler.Sender = (_, _) => throw new HttpRequestException("DNS Failure");

        // Act
        var result = await _sut.DeliverAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.ResponseReceived);
        Assert.False(result.TimedOut);
        Assert.True(result.ConnectionFailed);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)] // 429
    [InlineData(HttpStatusCode.InternalServerError)] // 500
    [InlineData(HttpStatusCode.BadRequest)] // 400
    [InlineData(HttpStatusCode.Redirect)] // 302 - Redirect test
    [InlineData(HttpStatusCode.MovedPermanently)] // 301 - Redirect test
    public async Task DeliverAsync_CapturesVariousStatusCodes(HttpStatusCode code)
    {
        // Arrange
        var request = CreateBaseRequest();
        _secretClientMock.GetSecretBase64Async(Arg.Any<string>()).Returns("dGVzdC1zZWNyZXQ=");

        _handler.Sender = (_, _) =>
        {
            // Assign, track, and return
            var response = new HttpResponseMessage(code);
            _trackedResponses.Add(response);
            return Task.FromResult(response);
        };

        // Act
        var result = await _sut.DeliverAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.ResponseReceived);
        Assert.Equal((int)code, result.HttpStatusCode);
    }

    [Fact]
    public async Task DeliverAsync_ThrowsOperationCanceledException_WhenCallerCancelsToken()
    {
        // Arrange
        var request = CreateBaseRequest();
        _secretClientMock
            .GetSecretBase64Async(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("dGVzdC1zZWNyZXQ=");

        _handler.Sender = async (_, ct) =>
        {
            // Simulate a hanging network request that respects cancellation
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage();
        };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Caller cancels the token, simulating app shutdown

        // Act & Assert
        // Verifies the requirement that the exception comes up natively rather than being swallowed into a WebhookDeliveryResult
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.DeliverAsync(request, cts.Token)
        );
    }

    private static WebhookDeliveryRequest CreateBaseRequest() =>
        new(
            SourceEventId: Guid.NewGuid(),
            EventType: LifecycleEventType.NhsNumberChanged,
            OccurredAt: DateTimeOffset.UtcNow,
            AffectedNhsNumber: "1234567890",
            DeliveryId: Guid.NewGuid(),
            DeliveryAttempt: 1,
            CorrelationId: Guid.NewGuid(),
            EndpointUrl: "https://supplier.com/webhook",
            KeyId: "key-1",
            SecretKeyVaultReference: "kv-ref"
        );

    public void Dispose()
    {
        // explicitly dispose all tracked mock responses
        foreach (var response in _trackedResponses)
        {
            response.Dispose();
        }

        _httpClient.Dispose();
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }

    // Light-weight in-memory HttpMessageHandler stub for testing
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        // Changed to nullable without default allocation
        public Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>
        >? Sender { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (Sender != null)
            {
                return Sender(request, cancellationToken);
            }

            // Explicitly scope the temporary instance and transfer ownership via a cloned response
            using var defaultResponse = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(new HttpResponseMessage(defaultResponse.StatusCode));
        }
    }
}
