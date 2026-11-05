using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Application.Models;
using SUI.NotificationService.Infrastructure.Entities;
using SUI.NotificationService.Infrastructure.Repositories;

namespace SUI.NotificationService.Infrastructure.UnitTests.Repositories;

public class WebhookReceiptRepositoryTests
{
    private readonly TableClient _tableClientMock;
    private readonly WebhookReceiptRepository _sut;

    public WebhookReceiptRepositoryTests()
    {
        _tableClientMock = Substitute.For<TableClient>();
        var logger = NullLogger<WebhookReceiptRepository>.Instance;

        _sut = new WebhookReceiptRepository(_tableClientMock, logger);
    }

    [Fact]
    public async Task AddAsync_CallsTableClientWithMappedEntity()
    {
        // Arrange
        var receipt = new WebhookDeliveryReceipt(
            SourceEventId: "evt-123",
            SupplierId: "sup-456",
            DeliveryId: Guid.NewGuid(),
            ContractVersion: "1.0",
            AcknowledgedAt: DateTimeOffset.UtcNow,
            HttpStatusCode: 200
        );

        // Act
        await _sut.AddAsync(receipt, CancellationToken.None);

        // Assert
        await _tableClientMock
            .Received(1)
            .AddEntityAsync(
                Arg.Is<WebhookDeliveryReceiptEntity>(e =>
                    e.PartitionKey != null
                    && e.RowKey != null
                    && e.SourceEventId == receipt.SourceEventId
                    && e.SupplierId == receipt.SupplierId
                    && e.HttpStatusCode == receipt.HttpStatusCode
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task AddAsync_SwallowsRequestFailedException_WhenStatusIs409Conflict()
    {
        // Arrange
        var receipt = new WebhookDeliveryReceipt(
            "evt",
            "sup",
            Guid.NewGuid(),
            "1",
            DateTimeOffset.UtcNow,
            200
        );

        var conflictException = new RequestFailedException(409, "EntityAlreadyExists");
        _tableClientMock
            .AddEntityAsync(Arg.Any<WebhookDeliveryReceiptEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(conflictException);

        // Act & Assert - Should not throw
        await _sut.AddAsync(receipt, CancellationToken.None);
    }

    [Fact]
    public async Task AddAsync_PropagatesRequestFailedException_WhenStatusIsNot409()
    {
        // Arrange
        var receipt = new WebhookDeliveryReceipt(
            "evt",
            "sup",
            Guid.NewGuid(),
            "1",
            DateTimeOffset.UtcNow,
            200
        );

        var authException = new RequestFailedException(403, "Forbidden");
        _tableClientMock
            .AddEntityAsync(Arg.Any<WebhookDeliveryReceiptEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(authException);

        // Act & Assert
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            _sut.AddAsync(receipt, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenEntityIsFound()
    {
        // Arrange
        var rawResponseMock = Substitute.For<Response>();
        rawResponseMock.Status.Returns(200);

        var responseMock = Substitute.For<Response<WebhookDeliveryReceiptEntity>>();
        responseMock.GetRawResponse().Returns(rawResponseMock);

        _tableClientMock
            .GetEntityAsync<WebhookDeliveryReceiptEntity>(default!, default!)
            .ReturnsForAnyArgs(Task.FromResult(responseMock));

        // Act
        var result = await _sut.ExistsAsync("evt-123", "sup-456", CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenStatusIs404NotFound()
    {
        // Arrange
        var notFoundException = new RequestFailedException(404, "NotFound");
        _tableClientMock
            .GetEntityAsync<WebhookDeliveryReceiptEntity>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _sut.ExistsAsync("evt-123", "sup-456", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_PropagatesException_WhenStatusIsNot404()
    {
        // Arrange
        var timeoutException = new RequestFailedException(408, "Timeout");
        _tableClientMock
            .GetEntityAsync<WebhookDeliveryReceiptEntity>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(timeoutException);

        // Act & Assert
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            _sut.ExistsAsync("evt-123", "sup-456", CancellationToken.None)
        );
    }
}
