using Interfaces;
using Microsoft.Azure.Functions.Worker;
using Models;

public sealed class GetProvidersActivity(ICustodianRegistry registry)
{
    private readonly ICustodianRegistry _registry = registry;

    [Function("GetProvidersActivity")]
    public Task<IReadOnlyList<ProviderDefinition>> Run([ActivityTrigger] object? _)
    {
        return Task.FromResult(_registry.GetCustodians());
    }
}
