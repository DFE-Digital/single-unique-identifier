using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.UnitTests.Fixtures;

namespace SUI.NotificationService.Application.UnitTests.Services;

public sealed class MeshMessageProcessorTests
{
    private readonly List<string> _messageIds = [];
    private readonly IMeshInboxClient _meshInboxClient = CreateMeshInboxClient();

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
        _meshInboxClient
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

    [Fact]
    public async Task AcknowledgeMessageAsync_AcknowledgesTheMessageOnTheMailbox()
    {
        using var cancellation = new CancellationTokenSource();

        await CreateProcessor().AcknowledgeMessageAsync("message-1", cancellation.Token);

        await _meshInboxClient.Received(1).AcknowledgeMessageAsync("message-1", cancellation.Token);
    }

    private Task<IReadOnlyList<PdsRecordChangeNotification>> ProcessAsync() =>
        CreateProcessor().ProcessMeshMessagesAsync(CancellationToken.None);

    private MeshMessageProcessor CreateProcessor() =>
        new(NullLogger<MeshMessageProcessor>.Instance, _meshInboxClient);

    private static IMeshInboxClient CreateMeshInboxClient()
    {
        var client = Substitute.For<IMeshInboxClient>();
        client.GetMessageIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        return client;
    }

    private void AddValidMessages(params (string MessageId, string? NhsNumber)[] messages)
    {
        foreach (var (messageId, nhsNumber) in messages)
        {
            AddValidMessage(messageId, nhsNumber);
        }
    }

    private void AddValidMessage(string messageId, string? nhsNumber = null) =>
        AddMessage(messageId, MeshNotificationFixtures.BuildNotification(nhsNumber));

    private void AddUnparseableMessage(string messageId, string content = "not a FHIR Bundle") =>
        AddMessage(messageId, content);

    private void AddUnreadableMessage(string messageId, Exception exception)
    {
        _meshInboxClient
            .ReadMessageAsync(messageId, Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
        TrackMessageId(messageId);
    }

    private void AddMessage(string messageId, string content)
    {
        _meshInboxClient
            .ReadMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(new MeshMailboxMessage(messageId, content));
        TrackMessageId(messageId);
    }

    // Re-stubs GetMessageIdsAsync with the running set, so adding a message is enough on its own -
    // callers never need a separate GetMessageIdsAsync setup call.
    private void TrackMessageId(string messageId)
    {
        _messageIds.Add(messageId);
        _meshInboxClient.GetMessageIdsAsync(Arg.Any<CancellationToken>()).Returns([.. _messageIds]);
    }
}
