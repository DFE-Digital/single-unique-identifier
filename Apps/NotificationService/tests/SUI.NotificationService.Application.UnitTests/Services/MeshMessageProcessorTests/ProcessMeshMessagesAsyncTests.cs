using NSubstitute;
using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.UnitTests.Services.MeshMessageProcessorTests;

public sealed class ProcessMeshMessagesAsyncTests : MeshMessageProcessorTestBase
{
    [Fact]
    public async Task ProcessMeshMessagesAsync_ReturnsEmpty_WhenMailboxIsEmpty()
    {
        var notifications = await ProcessAsync();

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_ReturnsMessageIdAndNhsNumber_ForEveryMessageRead()
    {
        AddValidMessages(("message-1", "9000000009"), ("message-2", "9000000017"));

        var notifications = await ProcessAsync();

        Assert.Equal(
            [
                new PdsRecordChangeNotification("message-1", "9000000009"),
                new PdsRecordChangeNotification("message-2", "9000000017"),
            ],
            notifications
        );
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_LeavesOutUnparseableMessages()
    {
        AddValidMessage("message-1", "9000000009");
        AddUnparseableMessage("message-unparseable");

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_LeavesOutMessagesCarryingNoNhsNumber()
    {
        AddValidMessage("message-1", "9000000009");
        AddValidMessage("message-no-nhs-number");

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_KeepsReadingTheMailbox_WhenAMessageCannotBeRead()
    {
        AddValidMessage("message-1", "9000000009");
        AddUnreadableMessage("message-unreadable", new HttpRequestException("MESH read failed"));

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_KeepsReadingTheMailbox_WhenAMessageReadTimesOut()
    {
        // Simulates HttpClient's own request timeout, which throws TaskCanceledException - an
        // OperationCanceledException unrelated to this execution's cancellation token.
        AddValidMessage("message-1", "9000000009");
        AddUnreadableMessage("message-timeout", new TaskCanceledException("The request timed out"));

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_PropagatesCancellation_WhenTheCallersTokenIsCancelled()
    {
        AddValidMessage("message-1", "9000000009");
        using var cancellation = new CancellationTokenSource();
        MeshInboxClient
            .ReadMessageAsync("message-cancelled", Arg.Any<CancellationToken>())
            .Returns<MeshMailboxMessage>(_ =>
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            });
        TrackMessageId("message-cancelled");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateProcessor().ProcessMeshMessagesAsync(cancellation.Token)
        );
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_Throws_WhenCancelled()
    {
        AddValidMessage("message-1", "9000000009");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateProcessor().ProcessMeshMessagesAsync(cancellation.Token)
        );
    }
}
