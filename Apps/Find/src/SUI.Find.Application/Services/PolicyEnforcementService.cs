using Microsoft.Extensions.Logging;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public class PolicyEnforcementService(ILogger<PolicyEnforcementService> logger)
    : IPolicyEnforcementService
{
    public Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType,
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTimeOffset.UtcNow;
        var modeString = request.Mode == ShareMode.Existence ? "EXISTENCE" : "CONTENT";
        var dataType = MapRecordTypeToDataType(request.RecordType, request.Mode);

        var matchedRule = dsaPolicy.Exceptions.FirstOrDefault(exception =>
            RuleMatches(exception, request, destOrgType, modeString, dataType, now)
        );

        var isException = matchedRule is not null;

        matchedRule ??= dsaPolicy.Defaults.FirstOrDefault(defaultRule =>
            RuleMatches(defaultRule, request, destOrgType, modeString, dataType, now)
        );

        if (matchedRule == null)
        {
            logger.LogInformation(
                "No matching rule found for {SourceOrg} -> {DestOrg}, dataType: {DataType}, mode: {Mode}, purpose: {Purpose}. Denying by default.",
                request.SourceOrgId,
                request.DestinationOrgId,
                dataType,
                request.Mode,
                request.Purpose
            );
            return Task.FromResult(
                new PolicyDecisionResult(false, "No matching rule found - denied by default")
            );
        }

        var isAllowed = string.Equals(
            matchedRule.Effect,
            "allow",
            StringComparison.OrdinalIgnoreCase
        );

        var ruleType = isException ? "exception" : "default";
        var reason = $"Matched {ruleType} rule: effect={matchedRule.Effect}, dataType={dataType}";

        // [Change to audit log entry]
        logger.LogInformation(
            "Policy decision for {SourceOrg} -> {DestOrg}: {Decision}. Reason: {Reason}",
            request.SourceOrgId,
            request.DestinationOrgId,
            isAllowed ? "ALLOWED" : "DENIED",
            reason
        );

        return Task.FromResult(new PolicyDecisionResult(isAllowed, reason));
    }

    public async Task<IReadOnlyList<SearchResultItem>> FilterResultsAsync(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<SearchResultItem> searchResultItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    )
    {
        var filtered = new List<SearchResultItem>();

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

            if (decision.IsAllowed)
            {
                filtered.Add(searchResultItem);
            }
        }

        logger.LogInformation(
            "Filtered {TotalCount} results to {AllowedCount} allowed results for {DestOrg}",
            searchResultItems.Count,
            filtered.Count,
            destOrgId
        );

        return filtered;
    }

    private static bool RuleMatches(
        DsaRuleDefinition rule,
        PolicyDecisionRequest request,
        string destOrgType,
        string modeString,
        string dataType,
        DateTimeOffset now
    )
    {
        if (!rule.Modes.Contains(modeString, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!rule.DataTypes.Contains(dataType, StringComparer.OrdinalIgnoreCase))
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

    private static string MapRecordTypeToDataType(string recordType, ShareMode mode)
    {
        // Extract the specific record type (part after the first dot if present)
        // e.g., "local-authority.children-social-care" → "children-social-care"
        //       "health.mental-health" → "mental-health"
        //       "crime-justice" → "crime-justice" (no change if no dot)
        var specificType = recordType.Contains('.')
            ? recordType.Substring(recordType.IndexOf('.') + 1)
            : recordType;

        // Normalize: replace dots and dashes with underscores
        var normalized = specificType.Replace(".", "_").Replace("-", "_");

        return mode == ShareMode.Existence ? $"{normalized}_ptr" : $"{normalized}_record";
    }
}
