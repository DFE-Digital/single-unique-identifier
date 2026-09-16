using System.Security.Cryptography;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.GetAnIdentifier.Infrastructure.Models;
using SUI.GetAnIdentifier.Infrastructure.Services;

namespace SUI.GetAnIdentifier.Infrastructure.IntegrationTests;

public class AuditServiceIntegrationTests : IAsyncLifetime
{
    private readonly AuditService _sut;
    private readonly BlobContainerClient _blobContainerClient;

    public AuditServiceIntegrationTests()
    {
        var logger = Substitute.For<ILogger<AuditService>>();
        const string blobName = "auditlogs";
        var blobContainerClient = new BlobContainerClient("UseDevelopmentStorage=true", blobName);
        _blobContainerClient = blobContainerClient;
        _sut = new AuditService(logger, _blobContainerClient);
    }

    public async Task InitializeAsync()
    {
        // Runs BEFORE every test: create the actual table in Azurite
        await _blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        // Runs AFTER every test: delete the table to keep Azurite clean
        await _blobContainerClient.DeleteAsync();
    }

    [Fact]
    public async Task AuditService_ShouldStoreCorrectValueInBlobStorage()
    {
        // Arrange
        string correlationId = Guid.NewGuid().ToString();
        var timestamp = new DateTimeOffset(2026, 09, 01, 08, 00, 00, TimeSpan.Zero);

        var auditEvent = new AuditEvent
        {
            EventName = "TestEvent",
            Timestamp = timestamp,
            CorrelationId = correlationId,
            TraceParent = "TestsTraceParent",
        };

        var expectedPrefix = $"{timestamp:yyyy/MM/dd}/{correlationId}_{timestamp.Ticks}.json";
        var auditHash = MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(auditEvent))
        );

        // Act
        await _sut.SendAuditEventAsync(auditEvent);

        // Assert
        foreach (var blobItem in _blobContainerClient.GetBlobs(prefix: expectedPrefix))
        {
            Assert.Equal(auditHash, blobItem.Properties.ContentHash);
        }
    }
}
