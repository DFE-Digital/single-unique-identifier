using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;

public class QueryProvidersFunction(ILogger<QueryProvidersFunction> logger)
{
    [Function(nameof(QueryProvidersFunction))]
    public async Task<SearchResultItem> QueryProvider([ActivityTrigger] QueryProviderInput data, FunctionContext context)
    {
        logger.LogInformation("Query Provider triggered for InstanceId: {InstanceId}", data.InstanceId);
        
        
        var result = new SearchResultItem
        (
            ProviderSystem: data.Provider.ProviderSystem,
            ProviderName: data.Provider.ProviderName,
            RecordType: "Test Record Type",
            RecordUrl: $"https://example.com/record/{data.Provider.OrgId}/{data.InstanceId}"
        );

        logger.LogInformation("Query Provider request completed for SUID: {InstanceID}", data.InstanceId);

        return result;
    }
}