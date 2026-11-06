using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application.UnitTests;

public class NotificationOrchestratorTests
{
    private readonly IMeshMessageProcessor _processorMock = Substitute.For<IMeshMessageProcessor>();
    private readonly ISupplierWebhookRepository _supplierRepoMock =
        Substitute.For<ISupplierWebhookRepository>();
    private readonly ISupplierWebhookDeliveryService _deliveryMock =
        Substitute.For<ISupplierWebhookDeliveryService>();
    private readonly IWebhookReceiptRepository _receiptRepoMock =
        Substitute.For<IWebhookReceiptRepository>();
    private readonly TimeProvider _timeMock = Substitute.For<TimeProvider>();
    private readonly NotificationOrchestrator _sut;

    public NotificationOrchestratorTests()
    {
        _sut = new NotificationOrchestrator(
            _processorMock,
            _supplierRepoMock,
            _deliveryMock,
            _receiptRepoMock,
            _timeMock,
            NullLogger<NotificationOrchestrator>.Instance
        );
    }

    [Fact]
    public async Task RunAsync_SkipsDelivery_WhenReceiptExists()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

        // Receipt exists
        _receiptRepoMock
            .ExistsAsync(
                notification.EventId.ToString(),
                supplier.SupplierId,
                Arg.Any<CancellationToken>()
            )
            .Returns(true);

        // Act
        await _sut.RunAsync(CancellationToken.None);

        // Assert
        await _deliveryMock.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);

        // Since it was skipped, it's considered resolved, so MESH is acknowledged
        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_AttemptsDeliveryAndWritesReceipt_OnSuccess()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Delivery succeeds
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
        await _sut.RunAsync(CancellationToken.None);

        // Assert - Delivery attempted
        await _deliveryMock
            .Received(1)
            .DeliverAsync(
                Arg.Is<WebhookDeliveryRequest>(req =>
                    req.SourceEventId == notification.EventId && req.EndpointUrl == "https://url"
                ),
                Arg.Any<CancellationToken>()
            );

        // Assert - Receipt written
        await _receiptRepoMock
            .Received(1)
            .AddAsync(
                Arg.Is<WebhookDeliveryReceipt>(rec =>
                    rec.SourceEventId == notification.EventId.ToString()
                    && rec.SupplierId == "sup-1"
                    && rec.HttpStatusCode == 200
                ),
                Arg.Any<CancellationToken>()
            );

        // Assert - MESH Acknowledged
        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DoesNotAckMesh_WhenAnyRetryableFailureOccurs()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplierA = new SupplierWebhook("sup-A", "https://url-a", true, "1", "secret");
        var supplierB = new SupplierWebhook("sup-B", "https://url-b", true, "1", "secret");
        _supplierRepoMock
            .GetAllEnabledAsync(Arg.Any<CancellationToken>())
            .Returns([supplierA, supplierB]);

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Supplier A Succeeds, Supplier B fails retryably (e.g. 503)
        _deliveryMock
            .DeliverAsync(
                Arg.Is<WebhookDeliveryRequest>(r => r.EndpointUrl == "https://url-a"),
                Arg.Any<CancellationToken>()
            )
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

        _deliveryMock
            .DeliverAsync(
                Arg.Is<WebhookDeliveryRequest>(r => r.EndpointUrl == "https://url-b"),
                Arg.Any<CancellationToken>()
            )
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
        await _sut.RunAsync(CancellationToken.None);

        // Assert
        await _receiptRepoMock
            .Received(1)
            .AddAsync(
                Arg.Is<WebhookDeliveryReceipt>(r => r.SupplierId == "sup-A"),
                Arg.Any<CancellationToken>()
            );
        await _receiptRepoMock
            .DidNotReceive()
            .AddAsync(
                Arg.Is<WebhookDeliveryReceipt>(r => r.SupplierId == "sup-B"),
                Arg.Any<CancellationToken>()
            );

        // MESH must NOT be Acknowledged because of Supplier B
        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_AcksMesh_WhenOnlyPermanentFailuresOccur()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Delivery fails permanently (e.g. 403 Forbidden)
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                new WebhookDeliveryResult(
                    DeliveryOutcome.PermanentFailure,
                    true,
                    403,
                    false,
                    false,
                    TimeSpan.Zero
                )
            );

        // Act
        await _sut.RunAsync(CancellationToken.None);

        // Assert - No receipt written
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);

        // Assert - MESH IS Acknowledged, because retrying a 403 is pointless
        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_GeneratesStableDeliveryId_ForSameEventAndSupplier()
    {
        // Arrange - Simulate processing the exact same event ID twice (e.g. a MESH redelivery)
        var eventId = Guid.NewGuid();
        var notification1 = CreateNotification("mesh-1", eventId);
        var notification2 = CreateNotification("mesh-2", eventId);

        // Set up the mock to return notification 1 on the first call, and notification 2 on the second
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification1], [notification2]);

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

        // Act - Run the orchestrator twice
        await _sut.RunAsync(CancellationToken.None);
        await _sut.RunAsync(CancellationToken.None);

        // Assert
        var calls = _deliveryMock
            .ReceivedCalls()
            .Where(c =>
                c.GetMethodInfo().Name == nameof(ISupplierWebhookDeliveryService.DeliverAsync)
            )
            .ToList();

        Assert.Equal(2, calls.Count);

        var request1 = (WebhookDeliveryRequest)calls[0].GetArguments()[0]!;
        var request2 = (WebhookDeliveryRequest)calls[1].GetArguments()[0]!;

        // The core AC: the generated DeliveryId MUST be mathematically identical
        Assert.Equal(request1.DeliveryId, request2.DeliveryId);
        Assert.NotEqual(Guid.Empty, request1.DeliveryId);
    }

    [Fact]
    public async Task RunAsync_PropagatesExceptionAndDoesNotAckMesh_WhenReceiptStoreFailsAfterDelivery()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

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

        // Simulate Azure Table Storage goes down right as it tries to save the success receipt
        _receiptRepoMock
            .AddAsync(Arg.Any<WebhookDeliveryReceipt>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Table storage offline"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RunAsync(CancellationToken.None)
        );

        // Ensure MESH was NEVER acknowledged, meaning it will remain in the inbox and be safely retried next run
        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_PropagatesCancellation_WhenTheCallersTokenIsCancelled()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        using var cancellation = new CancellationTokenSource();

        // Simulate the delivery service taking too long, and the token being cancelled mid-flight
        _deliveryMock
            .DeliverAsync(Arg.Any<WebhookDeliveryRequest>(), Arg.Any<CancellationToken>())
            .Returns<WebhookDeliveryResult>(_ =>
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.RunAsync(cancellation.Token)
        );

        // Ensure receipt was not saved and MESH was not Acknowledged due to the cancellation
        await _receiptRepoMock.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_DoesNothing_WhenNoMessagesToProcess()
    {
        // Arrange - Empty mailbox
        _processorMock.ProcessMeshMessagesAsync(Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.RunAsync(CancellationToken.None);

        // Assert - Should exit immediately without loading suppliers
        await _supplierRepoMock.DidNotReceiveWithAnyArgs().GetAllEnabledAsync(default);
    }

    [Fact]
    public async Task RunAsync_ReturnsEarly_WhenNoEnabledSuppliers()
    {
        // Arrange - Message exists, but no suppliers are enabled
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([]); // Empty

        // Act
        await _sut.RunAsync(CancellationToken.None);

        // Assert - The orchestrator bails out early, so no messages are Acknowledged and no deliveries are made.
        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
        await _deliveryMock.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_AcksMeshAndLogsWarning_OnUnknownDeliveryOutcome()
    {
        // Arrange
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([supplier]);

        _receiptRepoMock
            .ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Cast a fake integer to the Enum to force the switch statement into the 'default' block
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
        await _sut.RunAsync(CancellationToken.None);

        // Assert - Since there were 0 Retryable failures, the orchestrator safely defaults to Acknowledging the message.
        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id", Arg.Any<CancellationToken>());
    }

    private static PdsRecordChangeNotification CreateNotification(string meshId, Guid eventId) =>
        new(
            meshId,
            "1234567890",
            eventId,
            LifecycleEventType.PdsRecordChanged,
            DateTimeOffset.UtcNow
        );
}
