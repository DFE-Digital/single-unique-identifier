using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IJobQueueService
{
    Task<SearchJobDto> PostSearchJobAsync(
        SearchRequestMessage payload,
        CancellationToken cancellationToken = default
    );
}
