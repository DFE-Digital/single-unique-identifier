namespace SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

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
