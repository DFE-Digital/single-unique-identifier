using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public class QueryProvidersFunction(
    ILogger<QueryProvidersFunction> logger,
    IQueryProvidersService queryProvidersService
)
{
    [Function(nameof(QueryProvidersFunction))]
    public async Task<IReadOnlyList<SearchResultItem>> QueryProvider(
        [ActivityTrigger] FunctionContext context,
        QueryProviderInput data,
        CancellationToken cancellationToken
    )
    {
        using var logScope = logger.BeginScope("CorrelationId: {CorrelationId}", data.InvocationId);
        logger.LogInformation("Query Provider triggered");

        var results = await queryProvidersService.QueryProvidersAsync(data, cancellationToken);

        logger.LogInformation("Query Provider request completed");

        return results is { Success: true, Value: not null } ? results.Value : [];
    }
}
