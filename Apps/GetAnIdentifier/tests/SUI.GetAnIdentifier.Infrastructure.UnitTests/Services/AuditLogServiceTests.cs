using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.GetAnIdentifier.Infrastructure.Services;
using SUI.GetAnIdentifier.Infrastructure.UnitTests.Utility;

namespace SUI.GetAnIdentifier.Infrastructure.UnitTests.Services;

public class AuditLogServiceTests
{
    private readonly ILogger<AuditLogService> _logger = Substitute.For<ILogger<AuditLogService>>();
    private readonly BlobContainerClient _blobContainerClient =
        Substitute.For<BlobContainerClient>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    public AuditLogServiceTests()
    {
        _timeProvider
            .GetUtcNow()
            .Returns(_ => new DateTimeOffset(2026, 08, 31, 08, 00, 00, TimeSpan.Zero));
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

        var service = new AuditLogService(_logger, _blobContainerClient);

        // Act
        await service.LogIncomingRequestAsync(
            callerId: "test-caller",
            correlationId: "test-correlation-id",
            timestamp: _timeProvider.GetUtcNow(),
            httpMethod: "POST",
            requestPath: "/v1/get-an-identifier",
            requestData: new { Test = "Data" },
            cancellationToken: CancellationToken.None
        );

        // Assert
        _logger.VerifyLog(
            LogLevel.Information,
            "AuditLog [{EventType}] - CallerId: {CallerId}, CorrelationId: {CorrelationId}, Timestamp: {Timestamp}, HttpMethod: {HttpMethod}, RequestPath: {RequestPath}, StatusCode: {StatusCode}, ResponseSummary: {ResponseSummary}"
        );

        await blobClient
            .Received(1)
            .UploadAsync(Arg.Any<Stream>(), overwrite: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogOutgoingResponseAsync_LogsInformationWithoutException()
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

        var service = new AuditLogService(_logger, _blobContainerClient);

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
        _logger.VerifyLog(
            LogLevel.Information,
            "AuditLog [{EventType}] - CallerId: {CallerId}, CorrelationId: {CorrelationId}, Timestamp: {Timestamp}, HttpMethod: {HttpMethod}, RequestPath: {RequestPath}, StatusCode: {StatusCode}, ResponseSummary: {ResponseSummary}"
        );

        await blobClient
            .Received(1)
            .UploadAsync(Arg.Any<Stream>(), overwrite: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WritAuditLogAsync_HandlesAzureUploadExceptions()
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

        var service = new AuditLogService(_logger, _blobContainerClient);

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
        _logger.VerifyLog(
            LogLevel.Error,
            "Error occurred in azure services while attempting to write audit logs. Correlation ID: {CorrelationId}"
        );
    }

    [Fact]
    public async Task WritAuditLogAsync_HandlesBlobClientExceptions()
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

        var service = new AuditLogService(_logger, _blobContainerClient);

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
        _logger.VerifyLog(
            LogLevel.Error,
            "Error occurred in azure services while attempting to write audit logs. Correlation ID: {CorrelationId}"
        );
    }
}
