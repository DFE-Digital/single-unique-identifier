using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IJobResultsQueueClient
{
    Task SendAsync(JobResultMessage message, CancellationToken cancellationToken);
}
