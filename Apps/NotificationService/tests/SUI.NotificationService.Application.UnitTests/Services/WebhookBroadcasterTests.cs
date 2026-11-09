using Azure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application.UnitTests.Services;

public class WebhookBroadcasterTests
{
    private readonly ISupplierWebhookDeliveryService _deliveryMock;
    private readonly IWebhookReceiptRepository _receiptRepoMock;
    private readonly TimeProvider _timeMock;
    private readonly ILogger<WebhookBroadcaster> _loggerMock;
    private readonly WebhookBroadcaster _sut;

    public WebhookBroadcasterTests()
    {
        _deliveryMock = Substitute.For<ISupplierWebhookDeliveryService>();
        _receiptRepoMock = Substitute.For<IWebhookReceiptRepository>();
        _timeMock = Substitute.For<TimeProvider>();
        _loggerMock = Substitute.For<ILogger<WebhookBroadcaster>>();

        // Force logging branches to execute for full coverage
        _loggerMock.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _sut = new WebhookBroadcaster(_deliveryMock, _receiptRepoMock, _timeMock, _loggerMock);
    }

    [Fact]
    public async Task BroadcastAsync_ReturnsFalseAndSavesReceipt_OnSuccessfulDelivery()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.Success,
                    true,
                    200,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        var requiresRetry = await _sut.BroadcastAsync(
            notification,
            [supplier],
            CancellationToken.None
        );

        // Assert
        Assert.False(requiresRetry); // Success doesn't need retry
        await _deliveryMock
            .Received(1)
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>());
        await _receiptRepoMock
            .Received(1)
            .AddAsync(
                Arg.Is<WebhookDeliveryReceipt>(r =>
                    r.SupplierId == "sup-1" && r.HttpStatusCode == 200
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task BroadcastAsync_ReturnsFalseAndSkipsDelivery_WhenReceiptAlreadyExists()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var requiresRetry = await _sut.BroadcastAsync(
            notification,
            [supplier],
            CancellationToken.None
        );

        // Assert
        Assert.False(requiresRetry);
        await _deliveryMock.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastAsync_ReturnsTrue_WhenDeliveryFailsWithRetryableError()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.RetryableFailure,
                    true,
                    503,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        var requiresRetry = await _sut.BroadcastAsync(
            notification,
            [supplier],
            CancellationToken.None
        );

        // Assert
        Assert.True(requiresRetry); // Retryable failure requires retry
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastAsync_ReturnsFalse_WhenDeliveryFailsWithPermanentError()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.PermanentFailure,
                    true,
                    404,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        var requiresRetry = await _sut.BroadcastAsync(
            notification,
            [supplier],
            CancellationToken.None
        );

        // Assert
        Assert.False(requiresRetry); // Permanent failure should NOT be retried
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastAsync_ReturnsFalse_OnUnknownDeliveryOutcome()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    (DeliveryOutcome)999,
                    false,
                    0,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        var requiresRetry = await _sut.BroadcastAsync(
            notification,
            [supplier],
            CancellationToken.None
        );

        // Assert
        Assert.False(requiresRetry); // Default fallback should not trigger infinite retries
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastAsync_GeneratesSameDeliveryId_ForSameEventAndSupplier()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var notification = CreateNotification(eventId);
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.Success,
                    true,
                    200,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        await _sut.BroadcastAsync(notification, [supplier], CancellationToken.None);
        await _sut.BroadcastAsync(notification, [supplier], CancellationToken.None);

        // Assert
        var calls = _deliveryMock.ReceivedCalls().ToList();
        var request1 = (WebhookDeliveryRequest)calls[0].GetArguments()[0]!;
        var request2 = (WebhookDeliveryRequest)calls[1].GetArguments()[0]!;

        Assert.Equal(request1.DeliveryId, request2.DeliveryId);
    }

    [Fact]
    public async Task BroadcastAsync_PropagatesException_WhenReceiptStoreFailsAfterSuccessfulDelivery()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.Success,
                    true,
                    200,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Receipt Store goes down or throws an unexpected error
        _receiptRepoMock
            .AddAsync(Arg.Any<WebhookDeliveryReceipt>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException(500, "Azure Table Storage is down"));

        // Act & Assert
        // The Orchestrator MUST fail and should NOT Acknowledge the MESH message
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            _sut.BroadcastAsync(notification, [supplier], CancellationToken.None)
        );
    }

    [Fact]
    public async Task BroadcastAsync_HandlesMixedBroadcast_SkippingOneAndRetryingAnother()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplierSkipped = new SupplierWebhook(
            "sup-skipped",
            "https://url1",
            true,
            "1",
            "secret1"
        );
        var supplierRetried = new SupplierWebhook(
            "sup-retried",
            "https://url2",
            true,
            "1",
            "secret2"
        );

        // Setup Receipt Store: True for the first supplier, False for the second
        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), "sup-skipped", Arg.Any<CancellationToken>())
            .Returns(true);
        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), "sup-retried", Arg.Any<CancellationToken>())
            .Returns(false);

        // Retried one succeeds
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.Success,
                    true,
                    200,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        var requiresRetry = await _sut.BroadcastAsync(
            notification,
            [supplierSkipped, supplierRetried],
            CancellationToken.None
        );

        // Assert
        Assert.False(requiresRetry); // No failures, so no further retries required

        // Assert Skipped Supplier
        await _deliveryMock
            .DidNotReceive()
            .DeliverAsync(
                Arg.Is<WebhookDeliveryRequest>(r => r.EndpointUrl == "https://url1"),
                Arg.Any<CancellationToken>()
            );

        // Assert Retried Supplier
        await _deliveryMock
            .Received(1)
            .DeliverAsync(
                Arg.Is<WebhookDeliveryRequest>(r => r.EndpointUrl == "https://url2"),
                Arg.Any<CancellationToken>()
            );
        await _receiptRepoMock
            .Received(1)
            .AddAsync(
                Arg.Is<WebhookDeliveryReceipt>(r => r.SupplierId == "sup-retried"),
                Arg.Any<CancellationToken>()
            );
    }

    private static PdsRecordChangeNotification CreateNotification(Guid eventId) =>
        new(
            "mesh-id",
            "9000000009",
            eventId,
            LifecycleEventType.PdsRecordChanged,
            DateTimeOffset.UtcNow
        );
}
