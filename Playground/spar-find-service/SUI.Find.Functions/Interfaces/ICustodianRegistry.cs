using Models;

namespace Interfaces;

public interface ICustodianRegistry
{
    IReadOnlyList<ProviderDefinition> GetCustodians();
    ProviderDefinition GetCustodian(string orgId);
}
