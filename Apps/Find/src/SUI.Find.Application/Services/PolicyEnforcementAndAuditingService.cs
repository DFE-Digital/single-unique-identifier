using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.Application.Services;

public class PolicyEnforcementAndAuditingService(
    IAuditQueueClient auditQueueClient,
    TimeProvider timeProvider,
    ILogger<PolicyEnforcementAndAuditingService> logger
) : IPolicyEnforcementAndAuditingService
{
    public async Task<IReadOnlyList<PepResultItem<TItem>>> FilterItemsAndAuditAsync<TItem>(
        PepFilterAndAuditInput<TItem> input,
        CancellationToken cancellationToken
    )
        where TItem : IPepFilterable
    {
        var resultsWithDecision = await FilterItemsAsync(
            input.SourceOrgId,
            input.DestOrgId,
            input.DestOrgType,
            input.Items,
            input.DsaPolicy,
            input.Purpose
        );

        await CreateAndSendAuditMessageAsync(
            resultsWithDecision,
            input.DestOrgId,
            input.InvocationId,
            input.Purpose,
            cancellationToken
        );

        return resultsWithDecision;
    }

    public async Task CreateAndSendAuditMessageAsync<TItem>(
        IReadOnlyList<PepResultItem<TItem>> resultsWithDecision,
        string destinationOrgId,
        string invocationId,
        string purpose,
        CancellationToken cancellationToken
    )
        where TItem : IPepFilterable
    {
        logger.LogInformation(
            "Creating PEP audit log for {Count} results",
            resultsWithDecision.Count
        );

        var payload = new PepFindPayload
        {
            DestinationOrgId = destinationOrgId,
            Purpose = purpose,
            Mode = "EXISTENCE", // Currently hardcoded - all Find requests use Existence mode,
            Records = resultsWithDecision
                .Select(r => new PepFindRecordDetail
                {
                    SourceOrgId = r.SourceOrgId,
                    RecordUrl = r.Item is IPepFilterableRecord record ? record.RecordUrl : null,
                    RecordType = r.Item.RecordType,

                    IsSharedAllowed = r.Decision.IsAllowed,
                    RuleType = r.Decision.RuleType ?? "unknown",
                    RuleEffect = r.Decision.RuleEffect ?? "unknown",
                    RuleValidFrom = r.Decision.ValidFrom,
                    RuleValidUntil = r.Decision.ValidUntil,
                    DecisionReason = r.Decision.Reason,
                })
                .ToArray(),

            TotalRecordsFound = resultsWithDecision.Count,
            TotalRecordsShared = resultsWithDecision.Count(r => r.Decision.IsAllowed),
        };

        var auditMessage = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = ApplicationConstants.Audit.PolicyEnforcementPoint.FindEventName,
            ServiceName = "PolicyEnforcementPoint",
            Actor = new AuditActor { ActorId = destinationOrgId, ActorRole = "Organisation" },
            Payload = JsonSerializer.SerializeToElement(payload),
            Timestamp = timeProvider.GetUtcNow().DateTime,
            CorrelationId = invocationId,
        };

        await auditQueueClient.SendAuditEventAsync(auditMessage, cancellationToken);
    }

    private async Task<IReadOnlyList<PepResultItem<TItem>>> FilterItemsAsync<TItem>(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<TItem> pepFilterableItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose
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

            var decision = await EvaluateAsync(request, dsaPolicy, destOrgType);

            results.Add(
                new PepResultItem<TItem>(pepFilterableItem, sourceOrgId, destOrgId, decision)
            );
        }

        return results;
    }

    public Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType
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
