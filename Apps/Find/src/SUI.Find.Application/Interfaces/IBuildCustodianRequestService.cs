using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IBuildCustodianRequestService
{
    Task<Result<string>> BuildCustodianRequestAsync(BuildCustodianRequestDto request, CancellationToken cancellationToken);
}