using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.ProviderTriggers;

public class QueryProvidersFunction(ILogger<QueryProvidersFunction> logger)
{
    [Function(nameof(QueryProvidersFunction))]
    public async Task<IReadOnlyList<SearchResultItem>> QueryProvider(
        [ActivityTrigger] FunctionContext context,
        QueryProviderInput data
    )
    {
        using var logScope = logger.BeginScope("CorrelationId: {CorrelationId}", data.InvocationId);
        logger.LogInformation("Query Provider triggered");

        var result = new SearchResultItem(
            ProviderSystem: data.Provider.ProviderSystem,
            ProviderName: data.Provider.ProviderName,
            RecordType: "Test Record Type",
            RecordUrl: $"https://example.com/record/{data.Provider.OrgId}/{data.InstanceId}"
        );

        logger.LogInformation("Query Provider request completed");

        return [result];
    }
}
