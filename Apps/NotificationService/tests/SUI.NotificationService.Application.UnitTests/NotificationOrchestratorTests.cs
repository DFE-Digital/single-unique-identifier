using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;
using Xunit;

namespace SUI.NotificationService.Application.UnitTests;

public class NotificationOrchestratorTests
{
    private readonly IMeshMessageProcessor _processorMock;
    private readonly ISupplierWebhookRepository _supplierRepoMock;
    private readonly IWebhookBroadcaster _broadcasterMock;
    private readonly ILogger<NotificationOrchestrator> _loggerMock;
    private readonly NotificationOrchestrator _sut;

    public NotificationOrchestratorTests()
    {
        _processorMock = Substitute.For<IMeshMessageProcessor>();
        _supplierRepoMock = Substitute.For<ISupplierWebhookRepository>();
        _broadcasterMock = Substitute.For<IWebhookBroadcaster>();
        _loggerMock = Substitute.For<ILogger<NotificationOrchestrator>>();

        // Force logging branches to execute for 100% coverage
        _loggerMock.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _sut = new NotificationOrchestrator(
            _processorMock,
            _supplierRepoMock,
            _broadcasterMock,
            _loggerMock
        );
    }

    [Fact]
    public async Task RunAsync_DoesNothing_WhenNoMessagesToProcess()
    {
        _processorMock.ProcessMeshMessagesAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(CancellationToken.None);

        await _supplierRepoMock.DidNotReceiveWithAnyArgs().GetAllEnabledAsync(default);
        await _broadcasterMock
            .DidNotReceiveWithAnyArgs()
            .BroadcastBatchAsync(default!, default!, default);
    }

    [Fact]
    public async Task RunAsync_ReturnsEarly_WhenNoEnabledSuppliers()
    {
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(CancellationToken.None);

        await _broadcasterMock
            .DidNotReceiveWithAnyArgs()
            .BroadcastBatchAsync(default!, default!, default);
        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_AcksMessage_WhenBroadcasterReturnsMessageId()
    {
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        var suppliers = new List<SupplierWebhook> { supplier };
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns(suppliers);

        _broadcasterMock
            .BroadcastBatchAsync(
                Arg.Any<IReadOnlyList<PdsRecordChangeNotification>>(),
                Arg.Any<IReadOnlyList<SupplierWebhook>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(["mesh-id"]);

        await _sut.RunAsync(CancellationToken.None);

        await _processorMock
            .Received(1)
            .AcknowledgeMessageAsync("mesh-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DoesNotAckMessage_WhenBroadcasterReturnsEmptyList()
    {
        var notification = CreateNotification("mesh-id", Guid.NewGuid());
        _processorMock
            .ProcessMeshMessagesAsync(Arg.Any<CancellationToken>())
            .Returns([notification]);

        var supplier = new SupplierWebhook("sup-1", "https://url", true, "1", "secret");
        var suppliers = new List<SupplierWebhook> { supplier };
        _supplierRepoMock.GetAllEnabledAsync(Arg.Any<CancellationToken>()).Returns(suppliers);

        _broadcasterMock
            .BroadcastBatchAsync(
                Arg.Any<IReadOnlyList<PdsRecordChangeNotification>>(),
                Arg.Any<IReadOnlyList<SupplierWebhook>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);

        await _sut.RunAsync(CancellationToken.None);

        await _processorMock.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task RunAsync_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

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
