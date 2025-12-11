using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IFetchUrlStorageService
{
    Task AddAsync(AddFetchUrlRequest request, CancellationToken ct);
    Task<ResolvedFetchMappingResult> GetAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    );
}
