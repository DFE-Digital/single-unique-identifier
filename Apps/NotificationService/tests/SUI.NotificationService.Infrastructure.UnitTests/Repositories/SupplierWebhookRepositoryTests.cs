using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Infrastructure.Entities;
using SUI.NotificationService.Infrastructure.Repositories;

namespace SUI.NotificationService.Infrastructure.UnitTests.Repositories;

public class SupplierWebhookRepositoryTests
{
    private readonly TableClient _tableClientMock;
    private readonly ILogger<SupplierWebhookRepository> _loggerMock;
    private readonly SupplierWebhookRepository _sut;

    public SupplierWebhookRepositoryTests()
    {
        _tableClientMock = Substitute.For<TableClient>();
        _loggerMock = Substitute.For<ILogger<SupplierWebhookRepository>>();
        _sut = new SupplierWebhookRepository(_tableClientMock, _loggerMock);
    }

    [Theory]
    [InlineData("http://insecure.com/webhook")]
    [InlineData("ftp://files.com/webhook")]
    [InlineData("not-a-url")]
    public void DomainModel_ThrowsArgumentException_WhenUrlIsNotHttps(string invalidUrl)
    {
        Assert.Throws<ArgumentException>(() =>
            new SupplierWebhook("SUP123", invalidUrl, true, "1", "kv-ref")
        );
    }

    [Fact]
    public void DomainModel_CreatesSuccessfully_WhenUrlIsHttps()
    {
        var webhook = new SupplierWebhook(
            "SUP123",
            "https://secure.com/webhook",
            true,
            "1",
            "kv-ref"
        );
        Assert.Equal("https://secure.com/webhook", webhook.EndpointUrl);
    }

    [Fact]
    public async Task AddAsync_NormalisesRowKey_AndPreservesOriginalId()
    {
        // Arrange - using a messy ID with lowercase and a slash
        var messyId = "sys/admin#123";
        var expectedRowKey = "SYS_ADMIN_123";

        var webhook = new SupplierWebhook(
            messyId,
            "https://secure.com/webhook",
            true,
            "1",
            "kv-ref"
        );

        // Act
        await _sut.AddAsync(webhook);

        // Assert - prove it sends the normalized RowKey and keeps the Original ID
        await _tableClientMock
            .Received(1)
            .AddEntityAsync(
                Arg.Is<SupplierWebhookEntity>(e =>
                    e.RowKey == expectedRowKey && e.OriginalSupplierId == messyId
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task AddAsync_ThrowsInvalidOperationException_OnDuplicateSupplier()
    {
        var webhook = new SupplierWebhook(
            "SUP123",
            "https://secure.com/webhook",
            true,
            "1",
            "kv-ref"
        );
        var conflictException = new RequestFailedException(409, "Conflict");

        _tableClientMock
            .AddEntityAsync(Arg.Any<SupplierWebhookEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(conflictException);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddAsync(webhook));
    }

    [Fact]
    public async Task DisableAsync_NormalisesId_AndFlipsStatusToFalse()
    {
        // Arrange - using a messy ID
        var messyId = "sys/admin#123";
        var expectedRowKey = "SYS_ADMIN_123";

        var existingEntity = new SupplierWebhookEntity
        {
            RowKey = expectedRowKey,
            OriginalSupplierId = messyId,
            IsEnabled = true,
            ETag = new ETag("W/\"datetime'2026-09-07T00%3A00%3A00.0000000Z'\""),
        };

        var responseMock = Substitute.For<Response<SupplierWebhookEntity>>();
        responseMock.Value.Returns(existingEntity);

        // Mock GetEntityAsync to accept the NORMALIZED key
        _tableClientMock
            .GetEntityAsync<SupplierWebhookEntity>(
                SupplierWebhookEntity.DefaultPartitionKey,
                expectedRowKey,
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(responseMock));

        // Act - pass the messy ID
        await _sut.DisableAsync(messyId);

        // Assert
        await _tableClientMock
            .Received(1)
            .UpdateEntityAsync(
                Arg.Is<SupplierWebhookEntity>(e => !e.IsEnabled && e.RowKey == expectedRowKey),
                existingEntity.ETag,
                TableUpdateMode.Replace,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task EnableAsync_NormalisesId_AndFlipsStatusToTrue()
    {
        // Arrange - using a messy ID
        var messyId = "sys/admin#123";
        var expectedRowKey = "SYS_ADMIN_123";

        var existingEntity = new SupplierWebhookEntity
        {
            RowKey = expectedRowKey,
            OriginalSupplierId = messyId,
            IsEnabled = false, // Starting off disabled
            ETag = new ETag("W/\"datetime'2026-09-10T00%3A00%3A00.0000000Z'\""),
        };

        var responseMock = Substitute.For<Response<SupplierWebhookEntity>>();
        responseMock.Value.Returns(existingEntity);

        // Mock GetEntityAsync to accept the NORMALIZED key
        _tableClientMock
            .GetEntityAsync<SupplierWebhookEntity>(
                SupplierWebhookEntity.DefaultPartitionKey,
                expectedRowKey,
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(responseMock));

        // Act - pass the messy ID
        await _sut.EnableAsync(messyId);

        // Assert - verify it replaces the entity with IsEnabled = true
        await _tableClientMock
            .Received(1)
            .UpdateEntityAsync(
                Arg.Is<SupplierWebhookEntity>(e => e.IsEnabled && e.RowKey == expectedRowKey),
                existingEntity.ETag,
                TableUpdateMode.Replace,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GetAllEnabledAsync_QueriesDatabaseForEnabledOnly_AndMapsOriginalId()
    {
        // Note: The explicit filtering of disabled suppliers is tested under integration tests with actual Azure tables
        // This test solely checks if the repository method queries Azure as expected

        // Arrange - Define expected return data
        var enabledEntity1 = new SupplierWebhookEntity
        {
            RowKey = "SUP_001",
            OriginalSupplierId = "sup/001",
            EndpointUrl = "https://a.com",
            IsEnabled = true,
            ContractVersion = "1",
            SecretKeyVaultReference = "ref1",
        };

        var page = Page<SupplierWebhookEntity>.FromValues(
            [enabledEntity1],
            null,
            Substitute.For<Response>()
        );
        var asyncPageable = AsyncPageable<SupplierWebhookEntity>.FromPages([page]);

        // Capture the exact query string the repository sends to Azure
        string capturedODataFilter = string.Empty;

        _tableClientMock
            .QueryAsync<SupplierWebhookEntity>(
                Arg.Do<string>(filter => capturedODataFilter = filter), // Capture the query
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(asyncPageable);

        // Act
        var results = await _sut.GetAllEnabledAsync();
        var webhooks = results.ToList();

        // Assert 1: Prove the negative case
        // Verify the repository strictly instructs Azure to filter out disabled records.
        Assert.False(
            string.IsNullOrEmpty(capturedODataFilter),
            "Repository did not apply a filter."
        );
        Assert.Contains(
            "IsEnabled eq true",
            capturedODataFilter,
            StringComparison.OrdinalIgnoreCase
        );

        // Assert 2: Verify the domain mapping logic works on the returned data
        Assert.Single(webhooks);
        Assert.Equal("sup/001", webhooks[0].SupplierId);
        Assert.True(webhooks[0].IsEnabled);
    }
}
