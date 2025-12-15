using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IQueryProvidersService
{
    Task<Result<IReadOnlyList<SearchResultItem>>> QueryProvidersAsync(
        QueryProviderInput data,
        CancellationToken cancellationToken
    );
}
