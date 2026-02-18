using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public record AuditPepFindInput(
    PolicyContext PolicyContext,
    SearchJobMetadata Metadata,
    List<SearchResultWithDecision> SearchResultsWithDecisions
);

public class AuditPepFindActivity(
    ILogger<AuditPepFindActivity> logger,
    IAuditQueueClient auditQueueClient,
    TimeProvider timeProvider
)
{
    [Function(nameof(AuditPepFindActivity))]
    public async Task AuditPepFindAsync(
        [ActivityTrigger] AuditPepFindInput input,
        CancellationToken cancellationToken
    )
    {
        logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = input.Metadata.InvocationId }
        );
        logger.LogInformation(
            "Creating PEP audit log for {Count} results",
            input.SearchResultsWithDecisions.Count
        );

        // Build the payload
        var payload = new PepFindPayload
        {
            DestinationOrgId = input.PolicyContext.ClientId,
            Purpose = input.PolicyContext.Purpose,
            Mode = "EXISTENCE", // Currently hardcoded - all Find requests use Existence mode

            Records = input
                .SearchResultsWithDecisions.Select(r => new PepFindRecordDetail
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

            TotalRecordsFound = input.SearchResultsWithDecisions.Count,
            TotalRecordsShared = input.SearchResultsWithDecisions.Count(r => r.Decision.IsAllowed),
        };

        // Wrap in AuditEvent
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventName = ApplicationConstants.Audit.PolicyEnforcementPoint.FindEventName,
            ServiceName = "PolicyEnforcementPoint",
            Actor = new AuditActor
            {
                ActorId = input.PolicyContext.ClientId,
                ActorRole = "Organisation",
            },
            Payload = JsonSerializer.SerializeToElement(payload),
            Timestamp = timeProvider.GetUtcNow().DateTime,
            CorrelationId = input.Metadata.InvocationId,
        };

        // Write to audit service
        await auditQueueClient.SendAuditEventAsync(auditEvent, cancellationToken);
    }
}
