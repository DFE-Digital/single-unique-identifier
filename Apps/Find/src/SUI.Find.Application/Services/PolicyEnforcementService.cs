using Microsoft.Extensions.Logging;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.Application.Services;

public class PolicyEnforcementService(
    ILogger<PolicyEnforcementService> logger,
    TimeProvider timeProvider
) : IPolicyEnforcementService
{
    public Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType,
        CancellationToken cancellationToken = default
    )
    {
        var now = timeProvider.GetUtcNow();
        var modeString = request.Mode.ToString();
        var normalisedRecordType = request.RecordType.Replace(".", "_").Replace("-", "_");

        // First look for exceptions as these take precedence
        var matchedRule = dsaPolicy.Exceptions.FirstOrDefault(exception =>
            RuleMatches(exception, request, destOrgType, modeString, normalisedRecordType, now)
        );

        var isException = matchedRule is not null;

        // If no exception matched, look for default rules
        matchedRule ??= dsaPolicy.Defaults.FirstOrDefault(defaultRule =>
            RuleMatches(defaultRule, request, destOrgType, modeString, normalisedRecordType, now)
        );

        if (matchedRule == null)
        {
            logger.LogInformation(
                "No matching rule found for {SourceOrg} -> {DestOrg}, recordType: {RecordType}, mode: {Mode}, purpose: {Purpose}. Denying by default.",
                request.SourceOrgId,
                request.DestinationOrgId,
                normalisedRecordType,
                request.Mode,
                request.Purpose
            );
            return Task.FromResult(
                new PolicyDecisionResult
                {
                    IsAllowed = false,
                    Reason = "No matching rule found - denied by default",
                    RuleType = null,
                    RuleEffect = "deny",
                    ValidFrom = null,
                    ValidUntil = null,
                }
            );
        }

        var isAllowed = string.Equals(
            matchedRule.Effect,
            "allow",
            StringComparison.OrdinalIgnoreCase
        );

        var ruleType = isException ? "exception" : "default";
        var reason =
            $"Matched {ruleType} rule: effect={matchedRule.Effect}, recordType={normalisedRecordType}";

        logger.LogInformation(
            "Policy decision for {SourceOrg} -> {DestOrg}: {Decision}. Reason: {Reason}",
            request.SourceOrgId,
            request.DestinationOrgId,
            isAllowed ? "ALLOWED" : "DENIED",
            reason
        );

        return Task.FromResult(
            new PolicyDecisionResult
            {
                IsAllowed = isAllowed,
                Reason = reason,
                RuleType = ruleType,
                RuleEffect = matchedRule.Effect,
                ValidFrom = matchedRule.ValidFrom,
                ValidUntil = matchedRule.ValidUntil,
            }
        );
    }

    public async Task<IReadOnlyList<SearchResultWithDecision>> FilterResultsAsync(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<CustodianSearchResultItem> searchResultItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<SearchResultWithDecision>();

        foreach (var searchResultItem in searchResultItems)
        {
            var request = new PolicyDecisionRequest(
                sourceOrgId,
                destOrgId,
                searchResultItem.RecordType,
                ShareMode.Existence,
                purpose
            );

            var decision = await EvaluateAsync(request, dsaPolicy, destOrgType, cancellationToken);

            results.Add(new SearchResultWithDecision(searchResultItem, sourceOrgId, decision));
        }

        return results;
    }

    private bool RuleMatches(
        DsaRuleDefinition rule,
        PolicyDecisionRequest request,
        string destOrgType,
        string modeString,
        string recordType,
        DateTimeOffset now
    )
    {
        if (!rule.Modes.Contains(modeString, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!rule.RecordTypes.Contains(recordType, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!rule.Purposes.Contains(request.Purpose, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (rule.DestOrgIds.Count > 0)
        {
            if (
                !rule.DestOrgIds.Contains(
                    request.DestinationOrgId,
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                return false;
            }
        }
        else if (rule.DestOrgTypes.Count > 0)
        {
            if (!rule.DestOrgTypes.Contains(destOrgType, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        if (rule.ValidFrom.HasValue && now < rule.ValidFrom.Value)
        {
            return false;
        }

        if (rule.ValidUntil.HasValue && now > rule.ValidUntil.Value)
        {
            return false;
        }

        return true;
    }
}
