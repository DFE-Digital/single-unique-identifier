using Azure;
using Azure.Data.Tables;

namespace SUI.NotificationService.Infrastructure.Entities;

public class SupplierWebhookEntity : ITableEntity
{
    public const string DefaultPartitionKey = "SupplierWebhook";

    public string PartitionKey { get; set; } = DefaultPartitionKey;
    public string RowKey { get; set; } = string.Empty; // Normalised key
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string OriginalSupplierId { get; set; } = string.Empty; // Preserves the exact casing/characters

    public string EndpointUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string ContractVersion { get; set; } = string.Empty;
    public string SecretKeyVaultReference { get; set; } = string.Empty;
}
