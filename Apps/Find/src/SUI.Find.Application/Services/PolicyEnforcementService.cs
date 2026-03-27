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
        var recordType = request.RecordType;

        // First look for exceptions as these take precedence
        var matchedRule = dsaPolicy.Exceptions.FirstOrDefault(exception =>
            RuleMatches(exception, request, destOrgType, modeString, recordType, now)
        );

        var isException = matchedRule is not null;

        // If no exception matched, look for default rules
        matchedRule ??= dsaPolicy.Defaults.FirstOrDefault(defaultRule =>
            RuleMatches(defaultRule, request, destOrgType, modeString, recordType, now)
        );

        if (matchedRule == null)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "No matching rule found for {SourceOrg} -> {DestOrg}, recordType: {RecordType}, mode: {Mode}, purpose: {Purpose}. Denying by default.",
                    request.SourceOrgId,
                    request.DestinationOrgId,
                    recordType,
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
            $"Matched {ruleType} rule: effect={matchedRule.Effect}, recordType={recordType}";

        if (logger.IsEnabled(LogLevel.Information))
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

    public async Task<IReadOnlyList<PepResultItem<TItem>>> FilterItemsAsync<TItem>(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<TItem> pepFilterableItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    )
        where TItem : IPepFilterable
    {
        var results = new List<PepResultItem<TItem>>();

        foreach (var pepFilterableItem in pepFilterableItems)
        {
            var request = new PolicyDecisionRequest(
                sourceOrgId,
                destOrgId,
                pepFilterableItem.RecordType,
                ShareMode.Existence,
                purpose
            );

            var decision = await EvaluateAsync(request, dsaPolicy, destOrgType, cancellationToken);

            results.Add(
                new PepResultItem<TItem>(pepFilterableItem, sourceOrgId, destOrgId, decision)
            );
        }

        return results;
    }

    public async Task<IReadOnlyList<SearchResultWithDecision>> FilterResultsAsync(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<CustodianSearchResultItem> searchResultItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    ) =>
        (
            await FilterItemsAsync(
                sourceOrgId,
                destOrgId,
                destOrgType,
                searchResultItems,
                dsaPolicy,
                purpose,
                cancellationToken
            )
        )
            .Select(result => new SearchResultWithDecision(
                result.Item,
                result.SourceOrgId,
                result.DestOrgId,
                result.Decision
            ))
            .ToArray();

    private static bool RuleMatches(
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
