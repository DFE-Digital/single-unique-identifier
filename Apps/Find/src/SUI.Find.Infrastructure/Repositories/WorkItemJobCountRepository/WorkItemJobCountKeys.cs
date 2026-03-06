using SUI.Find.Infrastructure.Enums;

namespace SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

public static class WorkItemJobCountKeys
{
    public static string PartitionKey(string workItemId) =>
        $"WI_{TableKeyNormaliser.Normalise(workItemId)}";

    public static string RowKey(JobType jobType) =>
        $"JT_{TableKeyNormaliser.Normalise(jobType.ToString())}";
}
