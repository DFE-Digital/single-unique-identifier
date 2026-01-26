namespace SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

public static class RegisterKeys
{
    public static string PartitionKey(string sui) => $"SUI#{Normalise(sui)}";

    public static string RowKey(string custodianId, string recordType, string systemId) =>
        string.Join(
            "|",
            $"C#{Normalise(custodianId)}",
            $"RT#{Normalise(recordType)}",
            $"SYS#{Normalise(systemId)}"
        );

    private static string Normalise(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Key value cannot be null or empty.");
        }

        // 1.Trim
        value = value.Trim();

        // 2.Uppercase for consistency
        //value = value.ToUpperInvariant();

        // 3.Replace forbidden characters
        // return value
        //     .Replace("/", "_")
        //     .Replace("\\", "_")
        //     .Replace("#", "_")
        //     .Replace("?", "_");

        return value;
    }
}
