using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface ICustodianService
{
    Task<IReadOnlyList<ProviderDefinition>> GetCustodiansAsync();
    Task<IReadOnlyList<ProviderDefinition>> GetCustodianAsync(string orgId);
    
}
