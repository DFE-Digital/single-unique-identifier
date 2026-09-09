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
    public async Task GetAllEnabledAsync_ReturnsOnlyEnabledWebhooks_MappedWithOriginalId()
    {
        // Arrange - Ensure OriginalSupplierId is populated here!
        var enabledEntity1 = new SupplierWebhookEntity
        {
            RowKey = "SUP_001",
            OriginalSupplierId = "sup/001",
            EndpointUrl = "https://a.com",
            IsEnabled = true,
            ContractVersion = "1",
            SecretKeyVaultReference = "ref1",
        };
        var enabledEntity2 = new SupplierWebhookEntity
        {
            RowKey = "SUP_002",
            OriginalSupplierId = "sup/002",
            EndpointUrl = "https://b.com",
            IsEnabled = true,
            ContractVersion = "1",
            SecretKeyVaultReference = "ref2",
        };

        // Add a disabled entity to test the filtering
        var disabledEntity = new SupplierWebhookEntity
        {
            RowKey = "SUP_003",
            OriginalSupplierId = "sup/003",
            EndpointUrl = "https://c.com",
            IsEnabled = false, // This is the key property for the negative test
            ContractVersion = "1",
            SecretKeyVaultReference = "ref3",
        };

        var page = Page<SupplierWebhookEntity>.FromValues(
            [enabledEntity1, enabledEntity2, disabledEntity],
            null,
            Substitute.For<Response>()
        );
        var asyncPageable = AsyncPageable<SupplierWebhookEntity>.FromPages([page]);

        _tableClientMock
            .QueryAsync<SupplierWebhookEntity>(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(asyncPageable);

        // Act
        var results = await _sut.GetAllEnabledAsync();
        var webhooks = results.ToList();

        // Assert - verify it mapped the enabled ones and excluded the disabled one
        Assert.Equal(2, webhooks.Count);
        Assert.All(webhooks, w => Assert.True(w.IsEnabled));
        Assert.Contains(webhooks, w => w.SupplierId == "sup/001");
        Assert.Contains(webhooks, w => w.SupplierId == "sup/002");
        Assert.DoesNotContain(webhooks, w => w.SupplierId == "sup/003"); // Explicitly checking it was filtered
    }
}
