namespace SUI.Find.Infrastructure.Repositories.JobRepository;

public interface IJobRepository
{
    Task UpsertAsync(Job job, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> ListJobsByCustodianIdAsync(
        string custodianId,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default
    );
}
