using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Services;

public class QueryProvidersService(
    IBuildCustodianRequestService buildCustodianRequestService,
    ILogger<QueryProvidersService> logger,
    IMaskUrlService maskUrlService
    ) : IQueryProvidersService
{
    public async Task<Result<IReadOnlyList<SearchResultItem>>> QueryProvidersAsync(QueryProviderInput data, CancellationToken cancellationToken)
    {
        var requestDto = new BuildCustodianRequestDto(
            data.Provider,
            data.Suid
        );

        var searchResultItemsResponse = await buildCustodianRequestService.GetSearchResultItemsFromCustodianAsync(requestDto, cancellationToken);

        if (!searchResultItemsResponse.Success || searchResultItemsResponse.Value == null)
        {
            logger.LogInformation("Get SearchResultItems From custodian service returned null");
            return Result<IReadOnlyList<SearchResultItem>>.Fail("searchResultItemsResponse returned null");
        }

        logger.LogInformation("Starting masking service");

        var maskedSearchResultItems = await maskUrlService.CreateAsync(
            searchResultItemsResponse.Value,
            data,
            cancellationToken
        );

        return Result<IReadOnlyList<SearchResultItem>>.Ok(maskedSearchResultItems);

    }
}