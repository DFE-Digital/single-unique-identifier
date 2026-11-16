using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Infrastructure.Entities;
using SUI.NotificationService.Infrastructure.Repositories;
using SUI.NotificationService.Infrastructure.Utilities;

namespace SUI.NotificationService.Infrastructure.IntegrationTests;

public class WebhookReceiptRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly TableClient _tableClient;
    private readonly WebhookReceiptRepository _sut;
    private readonly string _tableName;

    public WebhookReceiptRepositoryIntegrationTests()
    {
        _tableName = $"TestReceipts{Guid.NewGuid():N}";
        _tableClient = new TableClient("UseDevelopmentStorage=true", _tableName);
        var logger = NullLogger<WebhookReceiptRepository>.Instance;
        _sut = new WebhookReceiptRepository(_tableClient, logger);
    }

    public async Task InitializeAsync()
    {
        await _tableClient.CreateIfNotExistsAsync();
    }

    public async Task DisposeAsync()
    {
        await _tableClient.DeleteAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsReceiptCorrectly_WithNormalizedKeys()
    {
        // Arrange
        var receipt = new WebhookDeliveryReceipt(
            SourceEventId: "evt/123",
            SupplierId: "sup/456",
            DeliveryId: Guid.NewGuid(),
            ContractVersion: "1.0",
            AcknowledgedAt: DateTimeOffset.Parse("2026-10-01T12:00:00Z"),
            HttpStatusCode: 200
        );

        // Act
        await _sut.AddAsync(receipt);

        // Assert - Read directly to prove keys and schema
        var expectedPartitionKey = TableKeyNormaliser.Normalise("evt/123");
        var expectedRowKey = TableKeyNormaliser.Normalise("sup/456");

        var response = await _tableClient.GetEntityAsync<WebhookDeliveryReceiptEntity>(
            expectedPartitionKey,
            expectedRowKey
        );

        var savedEntity = response.Value;
        Assert.Equal(receipt.SourceEventId, savedEntity.SourceEventId);
        Assert.Equal(receipt.SupplierId, savedEntity.SupplierId);
        Assert.Equal(receipt.DeliveryId, savedEntity.DeliveryId);
        Assert.Equal(receipt.HttpStatusCode, savedEntity.HttpStatusCode);
    }

    [Fact]
    public async Task AddAsync_IsIdempotent_AndSwallowsConflictExceptions()
    {
        // Arrange
        var receipt = new WebhookDeliveryReceipt(
            "event-1",
            "supplier-1",
            Guid.NewGuid(),
            "1.0",
            DateTimeOffset.UtcNow,
            202
        );

        await _sut.AddAsync(receipt); // First write succeeds

        // Act & Assert - Attempting to write the exact same keys should NOT throw
        var exception = await Record.ExceptionAsync(() => _sut.AddAsync(receipt));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenReceiptExists()
    {
        // Arrange
        var receipt = new WebhookDeliveryReceipt(
            "event-2",
            "supplier-2",
            Guid.NewGuid(),
            "1.0",
            DateTimeOffset.UtcNow,
            200
        );
        await _sut.AddAsync(receipt);

        // Act
        var exists = await _sut.ExistsAsync("event-2", "supplier-2");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenReceiptDoesNotExist()
    {
        // Act
        var exists = await _sut.ExistsAsync("non-existent-event", "supplier");

        // Assert
        Assert.False(exists);
    }
}
