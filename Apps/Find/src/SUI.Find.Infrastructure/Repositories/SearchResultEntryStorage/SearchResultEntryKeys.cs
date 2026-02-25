using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.Infrastructure.Repositories.SearchResultEntryStorage;

[ExcludeFromCodeCoverage(Justification = "Used only in infrastructure services.")]
public static class SearchResultEntryKeys
{
    public static string PartitionKey(string workItemId) => $"{Normalise(workItemId)}";

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
            $"C_{Normalise(custodianId)}",
            $"SYS_{Normalise(systemId)}",
            $"RT_{Normalise(recordType)}"
        );
    }

    private static string Normalise(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Key value cannot be null or empty.");
        }

        // 1.Trim
        value = value.Trim();

        // 2.Uppercase for consistency
        value = value.ToUpperInvariant();

        // 3.Replace forbidden characters
        return value.Replace("/", "_").Replace("\\", "_").Replace("#", "_").Replace("?", "_");
    }
}
