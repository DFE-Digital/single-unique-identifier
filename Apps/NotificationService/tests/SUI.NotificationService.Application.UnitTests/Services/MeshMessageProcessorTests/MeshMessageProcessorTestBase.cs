using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.UnitTests.Fixtures;

namespace SUI.NotificationService.Application.UnitTests.Services.MeshMessageProcessorTests;

public class MeshMessageProcessorTestBase
{
    private readonly List<string> _messageIds = [];
    internal readonly IMeshInboxClient MeshInboxClient = CreateMeshInboxClient();

    internal Task<IReadOnlyList<PdsRecordChangeNotification>> ProcessAsync() =>
        CreateProcessor().ProcessMeshMessagesAsync(CancellationToken.None);

    internal MeshMessageProcessor CreateProcessor() =>
        new(NullLogger<MeshMessageProcessor>.Instance, MeshInboxClient);

    internal static IMeshInboxClient CreateMeshInboxClient()
    {
        var client = Substitute.For<IMeshInboxClient>();
        client.GetMessageIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        return client;
    }

    internal void AddValidMessages(params (string MessageId, string? NhsNumber)[] messages)
    {
        foreach (var (messageId, nhsNumber) in messages)
        {
            AddValidMessage(messageId, nhsNumber);
        }
    }

    internal void AddValidMessage(string messageId, string? nhsNumber = null) =>
        AddMessage(messageId, MeshNotificationFixtures.BuildNotification(nhsNumber));

    internal void AddUnparseableMessage(string messageId, string content = "not a FHIR Bundle") =>
        AddMessage(messageId, content);

    internal void AddUnreadableMessage(string messageId, Exception exception)
    {
        MeshInboxClient
            .ReadMessageAsync(messageId, Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
        TrackMessageId(messageId);
    }

    internal void AddMessage(string messageId, string content)
    {
        MeshInboxClient
            .ReadMessageAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(new MeshMailboxMessage(messageId, content));
        TrackMessageId(messageId);
    }

    // Re-stubs GetMessageIdsAsync with the running set, so adding a message is enough on its own -
    // callers never need a separate GetMessageIdsAsync setup call.
    internal void TrackMessageId(string messageId)
    {
        _messageIds.Add(messageId);
        MeshInboxClient.GetMessageIdsAsync(Arg.Any<CancellationToken>()).Returns([.. _messageIds]);
    }
}
