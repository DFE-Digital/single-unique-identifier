using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Data.Tables;

namespace SUI.Find.Infrastructure.Models;

[ExcludeFromCodeCoverage(Justification = "Simple data model for Azure Table Storage")]
public sealed class FetchUrlMappingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string TargetUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string TargetOrgId { get; set; } = string.Empty;
    public string RequestingOrgId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;
}
