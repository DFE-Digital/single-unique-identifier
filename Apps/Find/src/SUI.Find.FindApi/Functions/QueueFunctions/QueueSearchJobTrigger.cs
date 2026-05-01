using System.Diagnostics;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.FindApi.Functions.QueueFunctions;

public class QueueSearchJobTrigger(
    ILogger<QueueSearchJobTrigger> logger,
    IJobRepository jobRepository,
    IWorkItemJobCountRepository workItemJobCountRepository,
    ICustodianService custodianService,
    IPolicyEnforcementService policyEnforcementService
)
{
    [Function(nameof(QueueSearchJobFunction))]
    public async Task QueueSearchJobFunction(
        [QueueTrigger(ApplicationConstants.SearchJobs.QueueName)]
            SearchRequestMessage searchRequestMessage,
        FunctionContext context,
        CancellationToken token
    )
    {
        using var activity = logger.StartActivityWithTraceParent(
            activityName: $"Handling_{nameof(SearchRequestMessage)}",
            searchRequestMessage.TraceParent,
            new Dictionary<string, object?>
            {
                { "WorkItemId", searchRequestMessage.WorkItemId },
                { "PersonId", searchRequestMessage.PersonId },
                { "RequestingOrganisationId", searchRequestMessage.RequestingOrganisationId },
                { "TraceParent", searchRequestMessage.TraceParent },
                { "TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty },
                { "InvocationId", context.InvocationId },
            }
        );

        logger.LogInformation(
            "QueueSearchJobTrigger function processed: Work item ID: {WorkItemId} for Requesting Organisation ID: {RequestingOrganisationId}",
            searchRequestMessage.WorkItemId,
            searchRequestMessage.RequestingOrganisationId
        );

        var custodians = await custodianService.GetCustodiansAsync();
        var requestingOrg = custodianService.GetCustodian(
            searchRequestMessage.RequestingOrganisationId,
            custodians
        );
        var createdJobs = 0;
        var pepFilteredCustodians = new List<PepResultItem<ProviderDefinition>>();

        foreach (var custodian in custodians)
        {
            pepFilteredCustodians.AddRange(
                await policyEnforcementService.FilterItemsAndAuditAsync(
                    new PepFilterInput<ProviderDefinition>(
                        SourceOrgId: custodian.OrgId,
                        DestOrgId: requestingOrg.OrgId,
                        DestOrgType: requestingOrg.OrgType,
                        Items: [custodian],
                        DsaPolicy: custodian.DsaPolicy,
                        Purpose: "SAFEGUARDING",
                        CorrelationId: searchRequestMessage.WorkItemId.ToString(),
                        searchRequestMessage.TraceParent
                    ),
                    token
                )
            );
        }

        foreach (var custodian in pepFilteredCustodians.Select(x => x.Item))
        {
            var custodianPayload = new CustodianLookupJobPayload(
                searchRequestMessage.PersonId,
                custodian.RecordType
            );
            var job = new Job
            {
                CustodianId = custodian.OrgId,
                RequestingOrganisationId = searchRequestMessage.RequestingOrganisationId,
                JobId = Guid.NewGuid().ToString(),
                JobType = JobType.CustodianLookup,
                PayloadJson = JsonSerializer.Serialize(custodianPayload),
                WorkItemId = searchRequestMessage.WorkItemId.ToString(),
                JobTraceParent = searchRequestMessage.TraceParent,
                WorkItemType = WorkItemType.SearchExecution,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            await jobRepository.UpsertAsync(job, token);
            createdJobs++;
        }

        logger.LogInformation("Created {NumOfJobs} jobs", createdJobs);

        var jobCountPayload = new SearchWorkItemPayload(searchRequestMessage.PersonId);
        await workItemJobCountRepository.UpsertAsync(
            new WorkItemJobCount
            {
                JobType = JobType.CustodianLookup,
                WorkItemId = searchRequestMessage.WorkItemId.ToString(),
                PayloadJson = JsonSerializer.Serialize(jobCountPayload),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ExpectedJobCount = createdJobs,
                RequestingOrganisationId = searchRequestMessage.RequestingOrganisationId,
            },
            token
        );
    }
}
