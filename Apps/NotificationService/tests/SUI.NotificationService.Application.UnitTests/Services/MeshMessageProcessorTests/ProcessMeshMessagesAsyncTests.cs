using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.Application.UnitTests.Fixtures;

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
    public async Task ProcessMeshMessagesAsync_ShouldIgnoreMessage_WhenItIsAKnownOtherEvent()
    {
        var logger = Substitute.For<ILogger<MeshMessageProcessor>>();
        AddValidMessage("message-1", "9000000009");
        AddMessage(
            "message-other",
            MeshNotificationFixtures.BuildNotificationWithEventType("nhs-number-change-2")
        );

        var notifications = await CreateProcessor(logger)
            .ProcessMeshMessagesAsync(CancellationToken.None);

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
        Assert.DoesNotContain(LoggedLevels(logger), level => level >= LogLevel.Warning);
        await MeshInboxClient.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_ShouldLogError_WhenEventTypeIsUnknown()
    {
        var logger = Substitute.For<ILogger<MeshMessageProcessor>>();
        AddMessage(
            "message-unknown",
            MeshNotificationFixtures.BuildNotificationWithEventType("pds-record-change-3")
        );

        await CreateProcessor(logger).ProcessMeshMessagesAsync(CancellationToken.None);
        Assert.Contains(LogLevel.Error, LoggedLevels(logger));
    }

    [Fact]
    public async Task ProcessMeshMessagesAsync_ShouldLogError_WhenMessageIsUnparseable()
    {
        var logger = Substitute.For<ILogger<MeshMessageProcessor>>();
        AddUnparseableMessage("message-bad");

        await CreateProcessor(logger).ProcessMeshMessagesAsync(CancellationToken.None);

        Assert.Contains(LogLevel.Error, LoggedLevels(logger));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("9000000008")]
    public async Task ProcessMeshMessagesAsync_LeavesOutMessagesWithoutAValidNhsNumber(
        string? nhsNumber
    )
    {
        AddValidMessage("message-1", "9000000009");
        AddValidMessage("message-without-valid-nhs-number", nhsNumber);

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
    public async Task ProcessMeshMessagesAsync_Propagates_WhenAMessageFailsUnexpectedly()
    {
        AddValidMessage("message-1", "9000000009");
        AddUnreadableMessage("message-faulty", new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(ProcessAsync);
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

    [Fact]
    public async Task ShouldRemoveDuplicates_FromReturnedList_WhenDuplicatesExist()
    {
        AddValidMessages(
            ("message-1", "9000000009"),
            ("message-2", "9000000017"),
            ("message-3", "9000000009"),
            ("message-4", "9000000017")
        );

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
    public async Task ShouldAcknowledgeEveryLaterCopy_AndNotTheNonDuplicate_WhenMessagesShareAnNhsNumber()
    {
        AddValidMessages(
            ("message-1", "9000000009"),
            ("message-2", "9000000009"),
            ("message-3", "9000000009"),
            ("message-4", "9000000017")
        );

        await ProcessAsync();

        await MeshInboxClient
            .Received(1)
            .AcknowledgeMessageAsync("message-2", Arg.Any<CancellationToken>());
        await MeshInboxClient
            .Received(1)
            .AcknowledgeMessageAsync("message-3", Arg.Any<CancellationToken>());
        await MeshInboxClient
            .DidNotReceive()
            .AcknowledgeMessageAsync("message-1", Arg.Any<CancellationToken>());
        await MeshInboxClient
            .DidNotReceive()
            .AcknowledgeMessageAsync("message-4", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldNotAcknowledgeAnything_WhenNoNhsNumberIsRepeated()
    {
        AddValidMessages(("message-1", "9000000009"), ("message-2", "9000000017"));
        AddUnparseableMessage("message-unparseable");
        AddValidMessage("message-no-nhs-number");

        await ProcessAsync();

        await MeshInboxClient.DidNotReceiveWithAnyArgs().AcknowledgeMessageAsync(default!, default);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    public async Task ShouldKeepAcknowledgingAndReturnNonDuplicates_WhenADuplicateAcknowledgementFails(
        Type exceptionType
    )
    {
        AddValidMessages(
            ("message-1", "9000000009"),
            ("message-2", "9000000009"),
            ("message-3", "9000000009")
        );
        MeshInboxClient
            .AcknowledgeMessageAsync("message-2", Arg.Any<CancellationToken>())
            .ThrowsAsync((Exception)Activator.CreateInstance(exceptionType)!);

        var notifications = await ProcessAsync();

        Assert.Equal([new PdsRecordChangeNotification("message-1", "9000000009")], notifications);
        await MeshInboxClient
            .Received(1)
            .AcknowledgeMessageAsync("message-3", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldPropagate_WhenADuplicateAcknowledgementFailsUnexpectedly()
    {
        AddValidMessages(("message-1", "9000000009"), ("message-2", "9000000009"));
        MeshInboxClient
            .AcknowledgeMessageAsync("message-2", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => ProcessAsync());
    }

    [Fact]
    public async Task ShouldStopAcknowledging_WhenCancelledDuringAcknowledgement()
    {
        AddValidMessages(
            ("message-1", "9000000009"),
            ("message-2", "9000000009"),
            ("message-3", "9000000009")
        );
        using var cancellation = new CancellationTokenSource();
        MeshInboxClient
            .AcknowledgeMessageAsync("message-2", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateProcessor().ProcessMeshMessagesAsync(cancellation.Token)
        );
        await MeshInboxClient
            .DidNotReceive()
            .AcknowledgeMessageAsync("message-3", Arg.Any<CancellationToken>());
    }

    private static IEnumerable<LogLevel> LoggedLevels(ILogger logger) =>
        logger
            .ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => (LogLevel)call.GetArguments()[0]!);
}
