using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.Application.Services;

public class PolicyEnforcementAndAuditingService(
    IPolicyEnforcementService pepService,
    IAuditQueueClient auditQueueClient,
    TimeProvider timeProvider,
    ILogger<PolicyEnforcementAndAuditingService> logger
) : IPolicyEnforcementAndAuditingService
{
    public async Task<
        IReadOnlyList<PepResultItem<CustodianSearchResultItem>>
    > FilterItemsAndAuditAsync(
        JobContext context,
        IReadOnlyList<CustodianSearchResultItem> records,
        string invocationId,
        string purpose,
        CancellationToken cancellationToken = default
    )
    {
        var resultsWithDecision = await pepService.FilterItemsAsync(
            context.Custodian.OrgId,
            context.SearchingOrganisation.OrgId,
            context.SearchingOrganisation.OrgType,
            records,
            context.Custodian.DsaPolicy,
            purpose,
            cancellationToken
        );

        await CreateAndSendAuditMessageAsync(
            resultsWithDecision,
            context.SearchingOrganisationId,
            invocationId,
            purpose,
            cancellationToken
        );

        return resultsWithDecision;
    }

    private async Task CreateAndSendAuditMessageAsync(
        IReadOnlyList<PepResultItem<CustodianSearchResultItem>> resultsWithDecision,
        string destinationOrgId,
        string invocationId,
        string purpose,
        CancellationToken cancellationToken
    )
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
                    RecordUrl = r.Item.RecordUrl,
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

    public async Task CreateAndSendAuditMessageAsync(
        AuditPepFindInput input,
        CancellationToken cancellationToken
    )
    {
        await CreateAndSendAuditMessageAsync(
            input.SearchResultsWithDecisions,
            input.PolicyContext.ClientId,
            input.Metadata.InvocationId,
            input.PolicyContext.Purpose,
            cancellationToken
        );
    }
}
