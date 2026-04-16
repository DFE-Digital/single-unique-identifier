using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.FindApi.Functions.ActivityFunctions;

public class AuditPepFindActivity(
    ILogger<AuditPepFindActivity> logger,
    IPolicyEnforcementAndAuditingService policyEnforcementAndAuditingService
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

        await policyEnforcementAndAuditingService.CreateAndSendAuditMessageAsync(
            input,
            cancellationToken
        );
    }
}
