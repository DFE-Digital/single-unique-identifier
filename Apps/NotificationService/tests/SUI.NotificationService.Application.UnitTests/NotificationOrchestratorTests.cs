using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application.UnitTests;

public class NotificationOrchestratorTests
{
    private readonly IMeshMessageProcessor _processorMock;
    private readonly IWebhookBroadcaster _broadcasterMock;
    private readonly ILogger<NotificationOrchestrator> _loggerMock;
    private readonly NotificationOrchestrator _sut;

    public NotificationOrchestratorTests()
    {
        _processorMock = Substitute.For<IMeshMessageProcessor>();
        _broadcasterMock = Substitute.For<IWebhookBroadcaster>();
        _loggerMock = Substitute.For<ILogger<NotificationOrchestrator>>();

        // Force logging branches to execute for 100% coverage
        _loggerMock.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _sut = new NotificationOrchestrator(_processorMock, _broadcasterMock, _loggerMock);
    }

    [Fact]
    public async Task RunAsync_DoesNothing_WhenNoMessagesToProcess()
    {
        _processorMock.ProcessMeshMessagesAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(CancellationToken.None);

        await _broadcasterMock.DidNotReceiveWithAnyArgs().BroadcastBatchAsync(default!, default);

        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_AcknowledgesMessage_WhenEndpointsRequiringNewDeliveryAttemptIsZero()
    {
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var aggregateResult = new BroadcastAggregateResult
        {
            EventId = notification.EventId,
            MessageId = notification.MessageId,
            TotalEnabledSuppliers = 1,
            NewlySuccessful = 1,
            FailedDeliveries = 0,
            SkippedPreviouslyAcknowledged = 0,
            EndpointsRequiringNewDeliveryAttempt = 0, // This triggers acknowledgement
            EndpointResults = [],
        };

        _broadcasterMock
            .BroadcastBatchAsync(
                Arg.Any<IReadOnlyList<PdsRecordChangeNotification>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([aggregateResult]);

        await _sut.RunAsync(CancellationToken.None);

        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DoesNotAcknowledgeMessage_WhenEndpointsRequiringNewDeliveryAttemptIsGreaterThanZero()
    {
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var aggregateResult = new BroadcastAggregateResult
        {
            EventId = notification.EventId,
            MessageId = notification.MessageId,
            TotalEnabledSuppliers = 1,
            NewlySuccessful = 0,
            FailedDeliveries = 1,
            SkippedPreviouslyAcknowledged = 0,
            EndpointsRequiringNewDeliveryAttempt = 1, // This blocks acknowledgement
            EndpointResults = [],
        };

        _broadcasterMock
            .BroadcastBatchAsync(
                Arg.Any<IReadOnlyList<PdsRecordChangeNotification>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([aggregateResult]);

        await _sut.RunAsync(CancellationToken.None);

        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_HandlesMixedBatch_AcknowledgingOnlyFullySuccessfulMessages()
    {
        var notification1 = CreateNotification("mesh-id-1", Guid.NewGuid());
        var notification2 = CreateNotification("mesh-id-2", Guid.NewGuid());

        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification1, notification2]);

        var successfulResult = new BroadcastAggregateResult
        {
            EventId = notification1.EventId,
            MessageId = notification1.MessageId,
            TotalEnabledSuppliers = 1,
            NewlySuccessful = 1,
            FailedDeliveries = 0,
            SkippedPreviouslyAcknowledged = 0,
            EndpointsRequiringNewDeliveryAttempt = 0, // Acknowledge this
            EndpointResults = [],
        };

        var failedResult = new BroadcastAggregateResult
        {
            EventId = notification2.EventId,
            MessageId = notification2.MessageId,
            TotalEnabledSuppliers = 1,
            NewlySuccessful = 0,
            FailedDeliveries = 1,
            SkippedPreviouslyAcknowledged = 0,
            EndpointsRequiringNewDeliveryAttempt = 1, // Keep this unacknowledged
            EndpointResults = [],
        };

        _broadcasterMock
            .BroadcastBatchAsync(
                Arg.Any<IReadOnlyList<PdsRecordChangeNotification>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([successfulResult, failedResult]);

        await _sut.RunAsync(CancellationToken.None);

        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id-1", Arg.Any<CancellationToken>());
        await _processorMock
            .DidNotReceive()
            .AcknowledgeMessageAsync("mesh-id-2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RunAsync(cancellation.Token)
        );
    }

    private static PdsRecordChangeNotification CreateNotification(string messageId, Guid eventId) =>
        new(
            messageId,
            "9000000009",
            eventId,
            LifecycleEventType.PdsRecordChanged,
            DateTimeOffset.UtcNow
        );
}
