using System.Buffers.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.Infrastructure.Clients;
using SUI.Find.Infrastructure.Factories;

namespace SUI.Find.Infrastructure.UnitTests.Clients;

public class AuditQueueClientTests
{
    private readonly AuditQueueClient _auditQueueClient;
    private readonly IQueueClientFactory _queueClientFactory =
        Substitute.For<IQueueClientFactory>();
    private readonly IAuditQueueSender _auditQueueSender = Substitute.For<IAuditQueueSender>();
    private readonly ILogger<AuditQueueClient> _logger = Substitute.For<
        ILogger<AuditQueueClient>
    >();

    public AuditQueueClientTests()
    {
        _auditQueueClient = new AuditQueueClient(_logger, _queueClientFactory);
        _queueClientFactory.GetAuditClient().Returns(_auditQueueSender);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendMessageToQueue()
    {
        // Arrange
        _auditQueueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _auditQueueClient.SendAuditEventAsync(GetMockAuditEvent, CancellationToken.None);

        // Assert
        _queueClientFactory.Received(1).GetAuditClient();
        await _auditQueueSender
            .Received(1)
            .SendMessageAsync(
                Arg.Is<string>(s => Base64.IsValid(s) && Convert.FromBase64String(s).Length > 1),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldLogErrorOnException()
    {
        // Arrange
        _auditQueueSender
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Queue send failed"));

        // Act
        await _auditQueueClient.SendAuditEventAsync(GetMockAuditEvent, CancellationToken.None);

        // Assert
        _queueClientFactory.Received(1).GetAuditClient();
        await _auditQueueSender
            .Received(1)
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _logger
            .Received()
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!
            );
    }

    private static AuditEvent GetMockAuditEvent =>
        new()
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = "TestEvent",
            ServiceName = "TestService",
            Actor = new AuditActor { ActorId = "TestActorId", ActorRole = "TestActorRole" },
            Payload = JsonSerializer.SerializeToElement(new { Data = "TestData" }),
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString(),
        };
}
