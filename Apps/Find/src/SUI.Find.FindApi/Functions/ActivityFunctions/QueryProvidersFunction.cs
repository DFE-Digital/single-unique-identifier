using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Repositories.SearchResultsRegister;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public class QueryProvidersFunction(
    ILogger<QueryProvidersFunction> logger,
    IQueryProvidersService queryProvidersService,
    IIdRegisterRepository idRegisterRepository,
    ISearchResultsRegisterRepository searchResultsRegisterRepository
)
{
    [Function(nameof(QueryProvidersFunction))]
    public async Task<IReadOnlyList<CustodianSearchResultItem>> QueryProvider(
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
            // 1. Upsert into ID Register
            await idRegisterRepository.UpsertAsync(
                new IdRegisterEntry()
                {
                    Sui = data.Suid,
                    CustodianId = data.Provider.OrgId,
                    RecordType = searchResultItem.RecordType,
                    SystemId = searchResultItem.SystemId ?? string.Empty,
                    CustodianSubjectId = searchResultItem.RecordId,
                    Provenance = Provenance.DiscoveredViaFanout,
                    LastIdDeliveredAtUtc = null,
                },
                cancellationToken
            );

            // 2. Persist to SearchResults Register for partial results
            await searchResultsRegisterRepository.AddAsync(
                new SearchResultsRegisterEntry
                {
                    CustodianId = data.Provider.OrgId,
                    SystemId = searchResultItem.SystemId ?? string.Empty,
                    RecordType = searchResultItem.RecordType,
                    RecordUrl = searchResultItem.RecordUrl,
                    SubmittedAtUtc = DateTimeOffset.UtcNow,
                    JobId = data.JobId,
                },
                cancellationToken
            );
        }

        logger.LogInformation("Query Provider request completed");

        return result.Value;
    }
}
