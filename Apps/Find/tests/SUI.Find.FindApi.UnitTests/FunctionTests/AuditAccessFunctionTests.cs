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
        var auditServiceMock = Substitute.For<IAuditService>();
        var accessMessage = new AuditAccessMessage
        {
            EventType = "TestEvent",
            ClientId = "TestClient",
            Timestamp = DateTime.UtcNow,
            Method = "GET",
            Path = "/test/path",
            CorrelationId = "12345",
            Suid = "suid-67890",
        };

        // Act
        var sut = new QueueAuditAccessTrigger(loggerMock, auditServiceMock);
        await sut.QueueAuditAccessFunction(accessMessage, null!);

        // Assert
        await auditServiceMock.Received(1).WriteAccessAuditLogAsync(accessMessage);
    }
}
