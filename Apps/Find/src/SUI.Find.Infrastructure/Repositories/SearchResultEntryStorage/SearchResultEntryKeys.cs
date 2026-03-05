namespace SUI.Find.Infrastructure.Repositories.SearchResultEntryStorage;

public static class SearchResultEntryKeys
{
    public static string PartitionKey(string workItemId) =>
        $"{TableKeyNormaliser.Normalise(workItemId)}";

    public static string RowKey(
        DateTimeOffset submittedAtUtc,
        string custodianId,
        string recordType,
        string systemId
    )
    {
        var ticksPrefix = submittedAtUtc.UtcTicks.ToString("D20");

        return string.Join(
            "|",
            ticksPrefix,
            $"C_{TableKeyNormaliser.Normalise(custodianId)}",
            $"SYS_{TableKeyNormaliser.Normalise(systemId)}",
            $"RT_{TableKeyNormaliser.Normalise(recordType)}"
        );
    }
}
