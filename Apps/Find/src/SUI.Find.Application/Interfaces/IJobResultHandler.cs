using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IJobResultHandler
{
    Task HandleAsync(
        JobResultMessage message,
        string invocationId,
        CancellationToken cancellationToken
    );
}
