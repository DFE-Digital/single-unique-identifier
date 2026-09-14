using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Infrastructure.Entities;
using SUI.NotificationService.Infrastructure.Repositories;
using SUI.NotificationService.Infrastructure.Utilities;

namespace SUI.NotificationService.Infrastructure.IntegrationTests;

public class SupplierWebhookRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly TableClient _tableClient;
    private readonly SupplierWebhookRepository _sut;
    private readonly string _tableName;

    public SupplierWebhookRepositoryIntegrationTests()
    {
        // Generate a unique table name per test instance for isolation
        // Table names must be alphanumeric and start with a letter
        _tableName = $"TestWebhooks{Guid.NewGuid():N}";

        // Connect to the local Azurite emulator
        _tableClient = new TableClient("UseDevelopmentStorage=true", _tableName);

        // Use NullLogger to prevent clutter in test output
        var logger = NullLogger<SupplierWebhookRepository>.Instance;

        _sut = new SupplierWebhookRepository(_tableClient, logger);
    }

    public async Task InitializeAsync()
    {
        // Runs BEFORE every test: create the actual table in Azurite
        await _tableClient.CreateIfNotExistsAsync();
    }

    public async Task DisposeAsync()
    {
        // Runs AFTER every test: delete the table to keep Azurite clean
        await _tableClient.DeleteAsync();
    }

    [Fact]
    public async Task AddAsync_ActuallyPersistsToDatabase_WithNormalisedKey()
    {
        // Arrange
        var webhook = new SupplierWebhook("sup/123", "https://test.com", true, "1", "kv-ref");

        // Act
        await _sut.AddAsync(webhook);

        // Assert - Read directly to prove that the record saved correctly
        var expectedRowKey = TableKeyNormaliser.Normalise("sup/123");
        var response = await _tableClient.GetEntityAsync<SupplierWebhookEntity>(
            SupplierWebhookEntity.DefaultPartitionKey,
            expectedRowKey
        );

        var savedEntity = response.Value;
        Assert.Equal("sup/123", savedEntity.OriginalSupplierId);
        Assert.Equal("https://test.com", savedEntity.EndpointUrl);
        Assert.True(savedEntity.IsEnabled);
    }

    [Fact]
    public async Task AddAsync_ThrowsInvalidOperationException_OnRealDuplicate()
    {
        // Arrange
        var webhook = new SupplierWebhook("SUP999", "https://test.com", true, "1", "kv");
        await _sut.AddAsync(webhook); // Add the first time

        // Act & Assert - This will throw a 409 Conflict
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AddAsync(webhook)
        );

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetAllEnabledAsync_QueriesRealDatabase_AndFiltersCorrectly()
    {
        // Arrange
        var enabled1 = new SupplierWebhook("SUP1", "https://a.com", true, "1", "kv");
        var disabled = new SupplierWebhook("SUP2", "https://b.com", false, "1", "kv");
        var enabled2 = new SupplierWebhook("SUP3", "https://c.com", true, "1", "kv");

        await _sut.AddAsync(enabled1);
        await _sut.AddAsync(disabled);
        await _sut.AddAsync(enabled2);

        // Act
        var results = (await _sut.GetAllEnabledAsync()).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, w => w.SupplierId == "SUP1");
        Assert.Contains(results, w => w.SupplierId == "SUP3");
        Assert.DoesNotContain(results, w => w.SupplierId == "SUP2");
    }

    [Fact]
    public async Task DisableAsync_UpdatesRealDatabaseRecord()
    {
        // Arrange
        var webhook = new SupplierWebhook("sys/admin", "https://test.com", true, "1", "kv");
        await _sut.AddAsync(webhook);

        // Act
        await _sut.DisableAsync("sys/admin");

        // Assert - Retrieve via our repository to ensure it maps the domain model correctly
        var allEnabled = await _sut.GetAllEnabledAsync();
        Assert.Empty(allEnabled); // The only entry is disabled!
    }

    [Fact]
    public async Task EnableAsync_UpdatesRealDatabaseRecord()
    {
        // Arrange - Save a webhook that starts off disabled
        var webhook = new SupplierWebhook("sys/admin", "https://test.com", false, "1", "kv");
        await _sut.AddAsync(webhook);

        // Verify it isn't returned initially
        var initiallyEnabled = await _sut.GetAllEnabledAsync();
        Assert.Empty(initiallyEnabled);

        // Act - Turn it back on
        await _sut.EnableAsync("sys/admin");

        // Assert - Prove the database updated and the query now finds it
        var allEnabled = (await _sut.GetAllEnabledAsync()).ToList();

        Assert.Single(allEnabled);
        Assert.Equal("sys/admin", allEnabled[0].SupplierId);
        Assert.True(allEnabled[0].IsEnabled);
    }

    [Fact]
    public async Task EnableAsync_ThrowsKeyNotFoundException_WhenWebhookDoesNotExist()
    {
        // Act & Assert - Attempting to enable an ID that was never added
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.EnableAsync("does/not/exist")
        );

        Assert.Contains("was not found", exception.Message);
    }
}
