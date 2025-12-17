using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Services;

public class QueryProvidersService(
    IBuildCustodianRequestService buildCustodianRequestService,
    ILogger<QueryProvidersService> logger,
    IMaskUrlService maskUrlService,
    IPolicyEnforcementPoint pep
) : IQueryProvidersService
{
    public async Task<Result<IReadOnlyList<SearchResultItem>>> QueryProvidersAsync(
        QueryProviderInput data,
        CancellationToken cancellationToken
    )
    {
        var requestDto = new BuildCustodianRequestDto(data.Provider, data.Suid);

        var searchResultItemsResponse =
            await buildCustodianRequestService.GetSearchResultItemsFromCustodianAsync(
                requestDto,
                cancellationToken
            );

        if (!searchResultItemsResponse.Success || searchResultItemsResponse.Value == null)
        {
            logger.LogInformation("Get SearchResultItems From custodian service returned null");
            return Result<IReadOnlyList<SearchResultItem>>.Fail(
                "searchResultItemsResponse returned null"
            );
        }

        var permittedItems = new List<SearchResultItem>();

        foreach (var item in searchResultItemsResponse.Value)
        {
            var request = new PolicyCheckRequest(
                SourceOrgId: data.Provider.OrgId,
                DestOrgId: data.RequestingOrg,
                DataType: item.RecordType,
                Purpose: "SAFEGUARDING", // im not sure where this would come from - will the search request include the purpose of the search?   
                Mode: "EXISTENCE" // mode is existence because we are querying the providers to see if any records exist with them - not asking to view them this would be FetchRecordServices s role
            );

            var decision = await pep.EvaluateDsaAsync(request);

            if (decision.IsAllowed)
            {
                permittedItems.Add(item);
            }
            else
            {
                // TODO review whether we need to log this out 
                logger.LogWarning("Record not permitted: Source Org {SourceOrg}: Requesting Org {RequestingOrg}: DataType {DataType}",
                    data.Provider.OrgId, data.RequestingOrg, item.RecordType);
            }
        }

        logger.LogInformation("Starting masking service");

        var maskedSearchResultItems = await maskUrlService.CreateAsync(
            permittedItems,
            data,
            cancellationToken
        );

        return Result<IReadOnlyList<SearchResultItem>>.Ok(maskedSearchResultItems);
    }
}
