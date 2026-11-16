using Azure;
using Azure.Data.Tables;

namespace SUI.NotificationService.Infrastructure.Entities;

public class WebhookDeliveryReceiptEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string SourceEventId { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public Guid DeliveryId { get; set; }
    public string ContractVersion { get; set; } = string.Empty;
    public DateTimeOffset AcknowledgedAt { get; set; }
    public int HttpStatusCode { get; set; }
}
