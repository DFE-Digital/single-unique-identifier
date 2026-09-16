using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.GetAnIdentifier.Infrastructure.Models;
using SUI.GetAnIdentifier.Infrastructure.Services;
using SUI.GetAnIdentifier.Infrastructure.UnitTests.Utility;

namespace SUI.GetAnIdentifier.Infrastructure.UnitTests.Services;

public class AuditServiceTests
{
    private readonly ILogger<AuditService> _logger = Substitute.For<ILogger<AuditService>>();
    private readonly BlobContainerClient _blobContainerClient =
        Substitute.For<BlobContainerClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly AuditEvent _auditEvent;

    public AuditServiceTests()
    {
        _timeProvider
            .GetUtcNow()
            .Returns(_ => new DateTimeOffset(2026, 08, 31, 08, 00, 00, TimeSpan.Zero));
        _auditEvent = new AuditEvent
        {
            CallerId = "test-caller",
            EventName = "test-event",
            CorrelationId = "test-correlation-id",
            Timestamp = _timeProvider.GetUtcNow(),
            TraceParent = "test-trace-parent",
            Method = "POST",
            Url = "/v1/get-an-identifier",
        };
    }

    [Fact]
    public async Task LogIncomingRequestAsync_LogsInformationWithoutException()
    {
        // Arrange
        var blobContentInfoResponse = Substitute.For<Response<BlobContentInfo>>();
        var blobClient = Substitute.For<BlobClient>();
        blobClient
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => blobContentInfoResponse);

        var blobContainerInfoResponse = Substitute.For<Response<BlobContainerInfo>>();
        _blobContainerClient
            .CreateIfNotExistsAsync()
            .ReturnsForAnyArgs(_ => blobContainerInfoResponse);
        _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(x => blobClient);

        var service = new AuditService(_logger, _blobContainerClient);

        // Act
        await service.SendAuditEventAsync(_auditEvent);

        // Assert
        _logger.VerifyLog(LogLevel.Information, JsonSerializer.Serialize(_auditEvent));

        await blobClient
            .Received(1)
            .UploadAsync(Arg.Any<Stream>(), overwrite: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WritAuditLogAsync_LogsAndThrowsAzureUploadExceptions()
    {
        // Arrange
        var blobClient = Substitute.For<BlobClient>();
        blobClient
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException("Azure service failure"));

        var blobContainerInfoResponse = Substitute.For<Response<BlobContainerInfo>>();
        _blobContainerClient
            .CreateIfNotExistsAsync()
            .ReturnsForAnyArgs(_ => blobContainerInfoResponse);
        _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(x => blobClient);

        var service = new AuditService(_logger, _blobContainerClient);

        // Act
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            service.SendAuditEventAsync(_auditEvent)
        );

        // Assert
        _logger.VerifyLog(
            LogLevel.Error,
            "Error occurred in azure services while attempting to write audit logs. Correlation ID: {CorrelationId}"
        );
    }

    [Fact]
    public async Task WritAuditLogAsync_LogsAndThrowsBlobClientExceptions()
    {
        // Arrange
        var blobContentInfoResponse = Substitute.For<Response<BlobContentInfo>>();
        var blobClient = Substitute.For<BlobClient>();
        blobClient
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => blobContentInfoResponse);

        _blobContainerClient
            .CreateIfNotExistsAsync()
            .Throws(new RequestFailedException("Azure service failure"));
        _blobContainerClient.GetBlobClient(Arg.Any<string>()).Returns(x => blobClient);

        var service = new AuditService(_logger, _blobContainerClient);

        // Act
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            service.SendAuditEventAsync(_auditEvent)
        );

        // Assert
        _logger.VerifyLog(
            LogLevel.Error,
            "Error occurred in azure services while attempting to write audit logs. Correlation ID: {CorrelationId}"
        );
    }

    [Fact]
    public async Task WritAuditLogAsync_LogsAndThrowsJsonExceptions()
    {
        // Arrange
        _blobContainerClient
            .CreateIfNotExistsAsync()
            .Throws(new NotSupportedException("JSON service failure")); // Actual JSON method is static, just testing handling of specific exception here.

        var service = new AuditService(_logger, _blobContainerClient);

        // Act
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            service.SendAuditEventAsync(_auditEvent)
        );

        // Assert
        _logger.VerifyLog(
            LogLevel.Error,
            "Error occurred during JSON serialization of audit entry. Correlation ID: {CorrelationId}"
        );
    }

    [Fact]
    public async Task WritAuditLogAsync_LogsAndThrowsOtherExceptions()
    {
        // Arrange
        _blobContainerClient
            .CreateIfNotExistsAsync()
            .Throws(new ApplicationException("Other unexpected service failure")); // Actual JSON method is static, just testing handling of specific exception here.

        var service = new AuditService(_logger, _blobContainerClient);

        // Act
        await Assert.ThrowsAsync<ApplicationException>(() =>
            service.SendAuditEventAsync(_auditEvent)
        );

        // Assert
        _logger.VerifyLog(
            LogLevel.Error,
            "Failed to write audit log to blob storage. Correlation ID: {CorrelationId}"
        );
    }
}
