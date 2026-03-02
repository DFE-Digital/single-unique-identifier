using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

[ExcludeFromCodeCoverage(Justification = "Used only in infrastructure services.")]
public static class RegisterKeys
{
    public static string PartitionKey(string sui) => $"SUI_{TableKeyNormaliser.Normalise(sui)}";

    public static string RowKey(string custodianId, string recordType, string systemId) =>
        string.Join(
            "|",
            $"C_{TableKeyNormaliser.Normalise(custodianId)}",
            $"RT_{TableKeyNormaliser.Normalise(recordType)}",
            $"SYS_{TableKeyNormaliser.Normalise(systemId)}"
        );
}
