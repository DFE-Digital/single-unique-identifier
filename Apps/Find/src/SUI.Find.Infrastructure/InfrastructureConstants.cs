using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.Infrastructure;

[ExcludeFromCodeCoverage(Justification = "Used only in infrastructure services.")]
public static class InfrastructureConstants
{
    public static class StorageTableAudit
    {
        public const string TableName = "AuditLogs";
    }

    public static class StorageTableUrlMappings
    {
        public const string TableName = "ResultsUrlMappings";
    }

    public static class StorageTableIdRegister
    {
        public const string TableName = "SuiCustodianRegister";
    }
}
