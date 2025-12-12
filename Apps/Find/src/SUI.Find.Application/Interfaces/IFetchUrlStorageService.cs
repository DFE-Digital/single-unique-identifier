using OneOf;
using OneOf.Types;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IFetchUrlStorageService
{
    Task AddAsync(AddFetchUrlRequest request, CancellationToken ct);
    Task<OneOf<ResolvedFetchMapping, NotFound, Unauthorized, Error>> GetAsync(
        string requestingOrg,
        string fetchId,
        CancellationToken ct
    );
}
