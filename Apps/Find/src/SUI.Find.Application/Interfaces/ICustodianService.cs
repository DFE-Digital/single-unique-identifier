using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface ICustodianService
{
    Task<IReadOnlyList<ProviderDefinition>> GetCustodiansAsync();
    Task<Result<ProviderDefinition>> GetCustodianAsync(string orgId);
    
}
