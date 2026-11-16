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
    private readonly ISupplierWebhookRepository _supplierRepoMock;
    private readonly ISupplierWebhookDeliveryService _deliveryMock;
    private readonly IWebhookReceiptRepository _receiptRepoMock;
    private readonly TimeProvider _timeMock;
    private readonly ILogger<WebhookBroadcaster> _loggerMock;
    private readonly WebhookBroadcaster _sut;

    public WebhookBroadcasterTests()
    {
        _supplierRepoMock = Substitute.For<ISupplierWebhookRepository>();
        _deliveryMock = Substitute.For<ISupplierWebhookDeliveryService>();
        _receiptRepoMock = Substitute.For<IWebhookReceiptRepository>();
        _timeMock = Substitute.For<TimeProvider>();
        _loggerMock = Substitute.For<ILogger<WebhookBroadcaster>>();

        // Force logging branches to execute for full coverage
        _loggerMock.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _sut = new WebhookBroadcaster(
            _supplierRepoMock,
            _deliveryMock,
            _receiptRepoMock,
            _timeMock,
            _loggerMock
        );
    }

    [Fact]
    public async Task BroadcastBatchAsync_ReturnsEmpty_WhenNoEnabledSuppliers()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Empty(acknowledgeableIds);
        await _deliveryMock.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastBatchAsync_ReturnsMessageIdAndSavesReceipt_OnSuccessfulDelivery()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

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
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Contains("mesh-id", acknowledgeableIds); // Success means it can be acknowledged
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
    public async Task BroadcastBatchAsync_ReturnsMessageIdAndSkipsDelivery_WhenReceiptAlreadyExists()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Contains("mesh-id", acknowledgeableIds);
        await _deliveryMock.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastBatchAsync_ReturnsEmpty_WhenDeliveryFailsWithRetryableError()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

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
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Empty(acknowledgeableIds); // Retryable failure requires retry, so not acknowledged
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastBatchAsync_ReturnsEmpty_WhenDeliveryFailsWithPermanentError()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

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
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Empty(acknowledgeableIds); // Permanent failure requires retry/fixing, so NOT acknowledged
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastBatchAsync_ReturnsMessageId_OnUnknownDeliveryOutcome()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

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
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Contains("mesh-id", acknowledgeableIds); // Default fallback should not trigger infinite retries
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task BroadcastBatchAsync_GeneratesSameDeliveryId_ForSameEventAndSupplier()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var notification = CreateNotification(eventId);
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

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
        await _sut.BroadcastBatchAsync([notification], CancellationToken.None);
        await _sut.BroadcastBatchAsync([notification], CancellationToken.None);

        // Assert
        var calls = _deliveryMock.ReceivedCalls().ToList();
        var request1 = (WebhookDeliveryRequest)calls[0].GetArguments()[0]!;
        var request2 = (WebhookDeliveryRequest)calls[1].GetArguments()[0]!;

        Assert.Equal(request1.DeliveryId, request2.DeliveryId);
    }

    [Fact]
    public async Task BroadcastBatchAsync_PropagatesException_WhenReceiptStoreFailsAfterSuccessfulDelivery()
    {
        // Arrange
        var notification = CreateNotification(Guid.NewGuid());
        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");

        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

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
            _sut.BroadcastBatchAsync([notification], CancellationToken.None)
        );
    }

    [Fact]
    public async Task BroadcastBatchAsync_HandlesMixedBroadcast_SkippingOneAndRetryingAnother()
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

        _supplierRepoMock
            .GetAllEnabledAsync(Arg.Any<CancellationToken>())
            .Returns([supplierSkipped, supplierRetried]);

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
        var acknowledgeableIds = await _sut.BroadcastBatchAsync(
            [notification],
            CancellationToken.None
        );

        // Assert
        Assert.Contains("mesh-id", acknowledgeableIds); // No failures, so no further retries required, safe to acknowledge

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
