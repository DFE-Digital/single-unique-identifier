using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public class QueryProvidersFunction(
    ILogger<QueryProvidersFunction> logger,
    IQueryProvidersService queryProvidersService,
    IIdRegisterRepository idRegisterRepository
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

        var result = await queryProvidersService.QueryProvidersAsync(data, cancellationToken);

        if (!result.Success || result.Value == null)
        {
            logger.LogInformation("No provider results returned");
            return [];
        }

        foreach (var searchResultItem in result.Value)
        {
            try
            {
                await idRegisterRepository.UpsertAsync(
                    new IdRegisterEntry()
                    {
                        Sui = data.Suid,
                        CustodianId = searchResultItem.ProviderId,
                        RecordType = searchResultItem.RecordType,
                        Provenance = Provenance.DiscoveredViaFanout,
                        LastIdDeliveredAtUtc = null,
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to upsert ID Register for Custodian {CustodianId}, RecordType {RecordType}",
                    searchResultItem.ProviderId,
                    searchResultItem.RecordType
                );
            }
        }

        logger.LogInformation("Query Provider request completed");

        return result.Value;
    }
}
