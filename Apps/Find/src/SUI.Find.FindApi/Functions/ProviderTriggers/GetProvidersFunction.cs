using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;

public class GetProvidersFunction(ILogger<GetProvidersFunction> logger)
{
    [Function(nameof(GetProvidersFunction))]

    public async Task<List<ProviderDefinition>> GetProviders([ActivityTrigger] string invocationId, string suid, FunctionContext context)
    {
        using var logScope = logger.BeginScope(
            "CorrelationId: {CorrelationId}", invocationId
        );

        logger.LogInformation("Get Providers triggered");

        var result = new List<ProviderDefinition>
        {
            new ProviderDefinition{
                OrgId = "12345",
                OrgName = "Test Provider 1",
                OrgType = "Local Authority 1",
                ProviderSystem = "Test System 1",
                ProviderName = "Test Provider Name 1"
            },
            new ProviderDefinition
            {
                OrgId = "678910",
                OrgName = "Test Provider 2",
                OrgType = "School" ,
                ProviderSystem = "Test System 2",
                ProviderName = "Test Provider Name 2"
            },
            new ProviderDefinition
            {
                OrgId = "111213",
                OrgName = "Test Provider 3",
                OrgType = "Police Force",
                ProviderSystem = "Test System",
                ProviderName = "Test Provider Name 3"
            }

        };

        logger.LogInformation("Get Providers request completed");

        return result;
    }
}