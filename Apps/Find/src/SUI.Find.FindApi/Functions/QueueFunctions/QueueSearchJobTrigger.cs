using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.FindApi.Functions.QueueFunctions;

public class QueueSearchJobTrigger(
    ILogger<QueueSearchJobTrigger> logger,
    IJobRepository jobRepository,
    IWorkItemJobCountRepository workItemJobCountRepository,
    ICustodianService custodianService
)
{
    [Function(nameof(QueueAuditAccessFunction))]
    public async Task QueueAuditAccessFunction(
        [QueueTrigger(ApplicationConstants.SearchJobs.QueueName)]
            SearchRequestMessage searchRequestMessage,
        FunctionContext context,
        CancellationToken token
    )
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                { "WorkItemId", searchRequestMessage.WorkItemId },
                { "PersonId", searchRequestMessage.PersonId },
                { "RequestingCustodianId", searchRequestMessage.RequestingCustodianId },
                { "TraceParent", context.TraceContext.TraceParent },
                { "TraceId", context.TraceContext.TraceState },
                { "InvocationId", context.InvocationId },
            }
        );
        logger.LogInformation(
            "QueueSearchJobTrigger function processed: Work item ID:{WorkItemId} for Custodian ID: {CustodianId}",
            searchRequestMessage.WorkItemId,
            searchRequestMessage.RequestingCustodianId
        );

        var custodians = await custodianService.GetCustodiansAsync();

        foreach (var custodian in custodians)
        {
            dynamic jsonPayload = new
            {
                Sui = searchRequestMessage.PersonId,
                RecordType = custodian.RecordType,
            };
            var job = new Job
            {
                CustodianId = custodian.OrgId,
                JobId = Guid.NewGuid().ToString(),
                JobType = JobType.CustodianLookup,
                PayloadJson = JsonSerializer.Serialize(jsonPayload),
                WorkItemId = searchRequestMessage.WorkItemId.ToString(),
                JobTraceParent = context.TraceContext.TraceParent,
                WorkItemType = WorkItemType.SearchExecution,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            await jobRepository.UpsertAsync(job, token);
        }

        dynamic jobCountPayload = new { Sui = searchRequestMessage.PersonId };
        await workItemJobCountRepository.UpsertAsync(
            new WorkItemJobCount
            {
                JobType = JobType.CustodianLookup,
                WorkItemId = searchRequestMessage.WorkItemId.ToString(),
                PayloadJson = JsonSerializer.Serialize(jobCountPayload),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                ExpectedJobCount = custodians.Count,
            },
            token
        );
    }
}
