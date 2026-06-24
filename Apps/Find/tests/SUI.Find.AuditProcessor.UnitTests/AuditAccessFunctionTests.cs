using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.AuditProcessor.Functions;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.AuditProcessor.UnitTests;

public class AuditAccessFunctionTests
{
    [Fact]
    public async Task ShouldCallAuditService_WhenValidModel()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<QueueAuditAccessTrigger>>();
        var accessMessage = new AuditAccessMessage
        {
            Method = "GET",
            Path = "/test/path",
            Suid = "suid-67890",
            ClientId = "client_id",
        };
        var auditServiceMock = Substitute.For<IAuditService>();
        var auditEvent = new AuditEvent
        {
            EventId = "eventId",
            EventName = "eventName",
            ServiceName = "serviceName",
            Actor = new AuditActor { ActorId = "someActorId", ActorRole = "someActor" },
            Payload = JsonSerializer.SerializeToElement(accessMessage),
            Timestamp = default,
            CorrelationId = Guid.NewGuid().ToString(),
        };
        var mockFunctionContext = Substitute.For<FunctionContext>();

        // Act
        var sut = new QueueAuditAccessTrigger(loggerMock, auditServiceMock);

        await sut.QueueAuditAccessFunction(auditEvent, mockFunctionContext, CancellationToken.None);

        // Assert
        await auditServiceMock
            .Received(1)
            .WriteAccessAuditLogAsync(auditEvent, Arg.Any<CancellationToken>());
    }
}
