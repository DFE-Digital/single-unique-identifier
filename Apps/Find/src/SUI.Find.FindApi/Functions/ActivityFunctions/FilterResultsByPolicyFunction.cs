using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public class FilterResultsByPolicyFunction(
    ILogger<FilterResultsByPolicyFunction> logger,
    IPolicyEnforcementService policyEnforcementService
)
{
    [Function(nameof(FilterResultsByPolicyFunction))]
    public async Task<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>> FilterResults(
        [ActivityTrigger] PepFilterInput<CustodianSearchResultItem> input,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "PEP filtering {Count} results from {SourceOrg} for {DestOrg}",
            input.Items.Count,
            input.SourceOrgId,
            input.DestOrgId
        );

        var decisionResults = await policyEnforcementService.FilterItemsAndAuditAsync(
            input,
            cancellationToken
        );

        return decisionResults;
    }
}
