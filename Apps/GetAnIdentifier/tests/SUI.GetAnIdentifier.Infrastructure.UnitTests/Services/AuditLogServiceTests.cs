using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.GetAnIdentifier.Infrastructure.Configuration;
using SUI.GetAnIdentifier.Infrastructure.Services;
using Xunit;

namespace SUI.GetAnIdentifier.Infrastructure.UnitTests.Services;

public class AuditLogServiceTests
{
    private readonly ILogger<AuditLogService> _logger = Substitute.For<ILogger<AuditLogService>>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly IOptions<AuditStorageOptions> _options;

    public AuditLogServiceTests()
    {
        _options = Options.Create(
            new AuditStorageOptions
            {
                ContainerName = "test-audit-logs",
                ConnectionString = string.Empty,
            }
        );
    }

    [Fact]
    public async Task LogIncomingRequestAsync_LogsInformationWithoutException()
    {
        // Arrange
        var service = new AuditLogService(_logger, _options, _configuration, null);

        // Act
        await service.LogIncomingRequestAsync(
            callerId: "test-caller",
            correlationId: "test-correlation-id",
            timestamp: DateTimeOffset.UtcNow,
            httpMethod: "POST",
            requestPath: "/v1/get-an-identifier",
            requestData: new { Test = "Data" },
            cancellationToken: CancellationToken.None
        );

        // Assert
        _logger
            .ReceivedWithAnyArgs(1)
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task LogOutgoingResponseAsync_LogsInformationWithoutException()
    {
        // Arrange
        var service = new AuditLogService(_logger, _options, _configuration, null);

        // Act
        await service.LogOutgoingResponseAsync(
            callerId: "test-caller",
            correlationId: "test-correlation-id",
            timestamp: DateTimeOffset.UtcNow,
            statusCode: 200,
            responseSummary: "Success",
            cancellationToken: CancellationToken.None
        );

        // Assert
        _logger
            .ReceivedWithAnyArgs(1)
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }
}
