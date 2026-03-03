using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.Infrastructure.Repositories.JobRepository;

[ExcludeFromCodeCoverage(
    Justification = "This will be covered after confirming implementation details"
)]
public class JobKeys
{
    public static string PartitionKey(string custodianId) =>
        $"C_{TableKeyNormaliser.Normalise(custodianId)}";

    public static string RowKey(DateTimeOffset createdAtUtc, string jobId)
    {
        var ticksPrefix = createdAtUtc.UtcTicks.ToString("D20");

        return $"{ticksPrefix}|JOB_{TableKeyNormaliser.Normalise(jobId)}";
    }
}
