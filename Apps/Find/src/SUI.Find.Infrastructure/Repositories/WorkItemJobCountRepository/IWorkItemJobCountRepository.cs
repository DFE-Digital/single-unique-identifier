using SUI.Find.Infrastructure.Enums;

namespace SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

public interface IWorkItemJobCountRepository
{
    Task UpsertAsync(
        WorkItemJobCount workItemJobCount,
        CancellationToken cancellationToken = default
    );

    Task<WorkItemJobCount?> GetByWorkItemIdAndJobTypeAsync(
        string workItemId,
        JobType jobType,
        CancellationToken cancellationToken = default
    );
}
