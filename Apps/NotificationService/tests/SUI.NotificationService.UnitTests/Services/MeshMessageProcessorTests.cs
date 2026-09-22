using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.UnitTests.Fixtures;

namespace SUI.NotificationService.UnitTests.Services;

public sealed class MeshMessageProcessorTests
{
    private readonly IMeshInboxClient _meshInboxClient = Substitute.For<IMeshInboxClient>();

    [Fact]
    public async Task ProcessMeshMessagesAsync_ReturnsEmpty_WhenMailboxIsEmpty()
    {
        GivenMailbox();

        var notifications = await ProcessAsync();

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_ReturnsMessageIdAndNhsNumber_ForEveryMessageRead()
    {
        GivenMailbox(("message-1", "9000000009"), ("message-2", "9000000017"));

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
        GivenMailbox(("message-1", "9000000009"));
        GivenMessage("message-unparseable", "not a FHIR Bundle");
        GivenMessageIds("message-unparseable", "message-1");

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_LeavesOutMessagesCarryingNoNhsNumber()
    {
        GivenMailbox(("message-1", "9000000009"));
        GivenMessage(
            "message-no-nhs-number",
            MeshNotificationFixtures.BuildNotification(nhsNumber: null)
        );
        GivenMessageIds("message-no-nhs-number", "message-1");

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_KeepsReadingTheMailbox_WhenAMessageCannotBeRead()
    {
        GivenMailbox(("message-1", "9000000009"));
        _meshInboxClient
            .ReadMessageAsync("message-unreadable", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("MESH read failed"));
        GivenMessageIds("message-unreadable", "message-1");

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_AcknowledgesNothing()
    {
        GivenMailbox(("message-1", "9000000009"));

        await ProcessAsync();

        await _meshInboxClient
            .DidNotReceive()
            .AcknowledgeMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_Throws_WhenCancelled()
    {
        GivenMailbox(("message-1", "9000000009"));
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

    private void GivenMailbox(params (string MessageId, string NhsNumber)[] messages)
    {
        foreach (var (messageId, nhsNumber) in messages)
        {
            GivenMessage(messageId, MeshNotificationFixtures.BuildNotification(nhsNumber));
        }

        GivenMessageIds(messages.Select(message => message.MessageId).ToArray());
    }

    private void GivenMessage(string messageId, string content) =>
        _meshInboxClient
            .ReadMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(new MeshMailboxMessage(messageId, content));

    private void GivenMessageIds(params string[] messageIds) =>
        _meshInboxClient
            .GetMessageIdsAsync(Arg.Any<CancellationToken>())
            .Returns(messageIds.ToList());
}
