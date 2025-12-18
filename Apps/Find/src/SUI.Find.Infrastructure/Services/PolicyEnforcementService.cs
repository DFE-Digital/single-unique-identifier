using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.Models.Policy;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.Services;

// TODO store policies and use cached versions
public class PolicyEnforcementService(ILogger<PolicyEnforcementService> logger, ICustodianService custodianService, IPolicyCompilerService policyService) : IPolicyEnforcementService
{
    private async Task<CompiledPolicyArtefact> GetPolicyArtefacts()
    {

        var providers = await custodianService.GetCustodiansAsync();

        CompiledPolicyArtefact policyArtefact = policyService.Compile(providers);

        logger.LogInformation("PEP Compiler started. Active DSA Agreements: {Count}", policyArtefact.AllowedRequests.Count);

        return policyArtefact;
    }


    public async Task<PolicyDecision> EvaluateDsaAsync(PolicyCheckRequest request)
    {
        logger.LogInformation("Evaluating DSA for Requesting Org - {OrgId}", request.DestOrgId);

        var policyArtefacts = await GetPolicyArtefacts();

        var key = PolicyKeyFactory.CreateKey(
            request.SourceOrgId,
            request.DestOrgId,
            request.Mode,
            request.DataType,
            request.Purpose
        );

        // just checking if key exists in Allowed Requests
        // Key would have been crated in the pep Compile method
        bool isAllowed = policyArtefacts.AllowedRequests.Contains(key);

        if (isAllowed)
        {
            logger.LogInformation("Request for Record Allowed: {VersionId}", policyArtefacts.PolicyVersionId);
            return new PolicyDecision(true, "Allowed", policyArtefacts.PolicyVersionId);
        }
        logger.LogInformation("Request for Record Denied: {VersionId}", policyArtefacts.PolicyVersionId);
        return new PolicyDecision(false, "Denied", policyArtefacts.PolicyVersionId);
    }

}

