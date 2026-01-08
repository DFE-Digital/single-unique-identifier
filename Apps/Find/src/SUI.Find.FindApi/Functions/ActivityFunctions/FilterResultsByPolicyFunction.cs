using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public class FilterResultsByPolicyFunction(
    ILogger<FilterResultsByPolicyFunction> logger,
    IPolicyEnforcementService policyEnforcementService
)
{
    [Function(nameof(FilterResultsByPolicyFunction))]
    public async Task<IReadOnlyList<SearchResultWithDecision>> FilterResults(
        [ActivityTrigger] FilterResultsInput input,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "PEP filtering {Count} results from {SourceOrg} for {DestOrg}",
            input.Items.Count,
            input.SourceOrgId,
            input.DestOrgId
        );

        var decisionResults = await policyEnforcementService.FilterResultsAsync(
            input.SourceOrgId,
            input.DestOrgId,
            input.DestOrgType,
            input.Items,
            input.DsaPolicy,
            input.Purpose,
            cancellationToken
        );

        return decisionResults;
    }
}
