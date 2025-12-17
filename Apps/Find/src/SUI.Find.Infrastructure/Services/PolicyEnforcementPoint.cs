using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.Services;

public class PolicyEnforcementPoint(ILogger<PolicyEnforcementPoint> logger, ICustodianService custodianService) : IPolicyEnforcementPoint
{
    private async Task<CompiledPolicyArtifact> GetPolicyArtifacts()
    {

        var providers = await custodianService.GetCustodiansAsync();

        CompiledPolicyArtifact policyArtifact = new PolicyCompiler().Compile(providers);

        logger.LogInformation("PEP Compiler started. Active DSA Agreements: {Count}", policyArtifact.AllowedRequests.Count);

        return policyArtifact;
    }


    public async Task<PolicyDecision> EvaluateDsaAsync(PolicyCheckRequest request)
    {
        logger.LogInformation("Evaluating DSA for Requesting Org - {OrgId}", request.DestOrgId);

        var policyArtifacts = await GetPolicyArtifacts();

        var key = PolicyKeyFactory.CreateKey(
            request.SourceOrgId,
            request.DestOrgId,
            request.Mode,
            request.DataType,
            request.Purpose
        );

        // just checking if key exists in Allowed Requests
        // Key would have been crated in the pep Compile method
        bool isAllowed = policyArtifacts.AllowedRequests.Contains(key);

        if (isAllowed)
        {
            logger.LogInformation("Request for Record Allowed: {VersionId}", policyArtifacts.PolicyVersionId);
            return new PolicyDecision(true, "Allowed", policyArtifacts.PolicyVersionId);
        }
        logger.LogInformation("Request for Record Denied: {VersionId}", policyArtifacts.PolicyVersionId);
        return new PolicyDecision(false, "Denied", policyArtifacts.PolicyVersionId);
    }

}

