namespace SUI.GetAnIdentifier.Infrastructure.Configuration;

public sealed class AuditStorageOptions
{
    public const string SectionName = "AuditStorage";

    public string ContainerName { get; set; } = "audit-logs";
    public string ConnectionString { get; set; } = string.Empty;
}
