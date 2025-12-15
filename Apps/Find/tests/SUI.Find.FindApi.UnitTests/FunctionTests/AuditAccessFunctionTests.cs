using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.FindApi.Functions.QueueFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class AuditAccessFunctionTests
{
    [Fact]
    public async Task ShouldCalAuditService_WhenValidModel()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<QueueAuditAccessTrigger>>();
        var accessMessage = new AuditAccessMessage
        {
            Method = "GET",
            Path = "/test/path",
            Suid = "suid-67890",
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

        // Act
        var sut = new QueueAuditAccessTrigger(loggerMock, auditServiceMock);
        await sut.QueueAuditAccessFunction(auditEvent, null!, CancellationToken.None);

        // Assert
        await auditServiceMock
            .Received(1)
            .WriteAccessAuditLogAsync(auditEvent, Arg.Any<CancellationToken>());
    }
}
