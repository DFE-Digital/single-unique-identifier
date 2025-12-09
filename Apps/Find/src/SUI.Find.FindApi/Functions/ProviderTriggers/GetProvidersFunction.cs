using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;

public class GetProvidersFunction(
    ILogger<GetProvidersFunction> logger,
    ICustodianService custodianService
)
{
    [Function(nameof(GetProvidersFunction))]
    public async Task<IReadOnlyList<ProviderDefinition>> GetProviders(
        [ActivityTrigger] string invocationId,
        string suid
    )
    {
        using var logScope = logger.BeginScope("CorrelationId: {CorrelationId}", invocationId);

        logger.LogInformation("Get Providers triggered");

        var results = await custodianService.GetCustodiansAsync();

        logger.LogInformation("Get Providers request completed");

        return results;
    }
}
