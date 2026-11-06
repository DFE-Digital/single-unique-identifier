using Azure;
using SUI.NotificationService.Infrastructure.Entities;

namespace SUI.NotificationService.Infrastructure.UnitTests.Entities;

public class WebhookDeliveryReceiptEntityTests
{
    [Fact]
    public void Properties_CanBeExplicitlySetAndRetrieved()
    {
        // Arrange
        var entity = new WebhookDeliveryReceiptEntity();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act - Explicitly call every setter
        entity.PartitionKey = "pk-123";
        entity.RowKey = "rk-456";
        entity.SourceEventId = "event-1";
        entity.SupplierId = "sup-1";
        entity.DeliveryId = id;
        entity.ContractVersion = "1.0";
        entity.AcknowledgedAt = now;
        entity.HttpStatusCode = 200;
        entity.ETag = new ETag("etag-1");
        entity.Timestamp = now;

        // Assert - Explicitly call every getter
        Assert.Equal("pk-123", entity.PartitionKey);
        Assert.Equal("rk-456", entity.RowKey);
        Assert.Equal("event-1", entity.SourceEventId);
        Assert.Equal("sup-1", entity.SupplierId);
        Assert.Equal(id, entity.DeliveryId);
        Assert.Equal("1.0", entity.ContractVersion);
        Assert.Equal(200, entity.HttpStatusCode);
        Assert.Equal(now, entity.Timestamp);
        Assert.Equal(new ETag("etag-1"), entity.ETag);
    }
}
