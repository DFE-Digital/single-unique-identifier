using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.Models.Policy;

namespace SUI.Find.Application.Services;

public class QueryProvidersService(
    IBuildCustodianRequestService buildCustodianRequestService,
    ILogger<QueryProvidersService> logger,
    IMaskUrlService maskUrlService,
    IPolicyEnforcementService pepService
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
                // TODO remove hardcoding of purpose
                Purpose: "SAFEGUARDING", // As discussed with Simon - leaving this with a to do once its decided how purpose will be passed through
                Mode: "EXISTENCE"
            );

            var decision = await pepService.EvaluateDsaAsync(request);

            if (decision.IsAllowed)
            {
                logger.LogInformation("Record permitted for Requesting Org {RequestingOrg} with Policy Version {PolicyVersionId}", data.RequestingOrg, decision.PolicyVersionId);
                permittedItems.Add(item);
            }
            else
            {
                logger.LogInformation("Record not permitted for Requesting Org {RequestingOrg} with Policy Version {PolicyVersionId}", data.RequestingOrg, decision.PolicyVersionId);
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
