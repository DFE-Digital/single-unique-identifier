using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IJobResultHandler
{
    Task HandleAsync(JobResultMessage message, CancellationToken cancellationToken);
}
