using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;

public class GetProvidersFunction(ILogger<GetProvidersFunction> logger)
{
    [Function(nameof(GetProvidersFunction))]

    public async Task<List<ProviderDefinition>> GetProviders([ActivityTrigger] string suid, FunctionContext context)
    {
        logger.LogInformation("Get Providers triggered for SUID: {Suid}", suid);

        var result = new List<ProviderDefinition>
        {
            new ProviderDefinition(
                OrgId: "12345",
                OrgName: "Test Provider 1",
                OrgType: "Local Authority 1",
                ProviderSystem: "Test System 1",
                ProviderName: "Test Provider Name 1"
            ),
            new ProviderDefinition(
                OrgId: "678910",
                OrgName: "Test Provider 2",
                OrgType: "School" ,
                ProviderSystem: "Test System 2",
                ProviderName: "Test Provider Name 2"
            ),
            new ProviderDefinition(
                OrgId: "111213",
                OrgName: "Test Provider 3",
                OrgType: "Police Force",
                ProviderSystem: "Test System",
                ProviderName: "Test Provider Name 3"
            )

        };

        logger.LogInformation("Get Providers request completed for SUID: {Suid}", suid);

        return result;
    }
}