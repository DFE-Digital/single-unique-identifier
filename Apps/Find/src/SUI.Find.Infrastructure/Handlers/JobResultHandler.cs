using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.Handlers;

public class JobResultHandler(
    ILogger<JobResultHandler> logger,
    IJobProcessorService jobService,
    IIdRegisterRepository idRegisterRepository,
    IWorkItemJobCountRepository workItemJobCountRepository,
    ICustodianService custodianService,
    IPolicyEnforcementService pepFilteringService,
    ISearchResultEntryRepository searchResultRepository
) : IJobResultHandler
{
    public async Task HandleAsync(JobResultMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Handling job results for JobId {JobId} WorkItemId {WorkItemId}",
            message.JobId,
            message.WorkItemId
        );

        // Processing behaviour determined by JobType
        if (message.JobType != JobType.CustodianLookup)
        {
            logger.LogInformation(
                "Skipping processing for unsupported JobType {JobType}",
                message.JobType
            );

            await jobService.MarkCompletedAsync(message.JobId, cancellationToken);
            return;
        }

        var records = message.Records;

        if (records.Count == 0)
        {
            logger.LogInformation("No records submitted for JobId {JobId}", message.JobId);

            await jobService.MarkCompletedAsync(message.JobId, cancellationToken);
            return;
        }

        // Update ID Register with ALL records before PEP filtering
        logger.LogInformation(
            "Upserting {Count} records into ID Register for WorkItemId {WorkItemId}",
            records.Count,
            message.WorkItemId
        );

        var associatedWorkItemJobCount =
            await workItemJobCountRepository.GetByWorkItemIdAndJobTypeAsync(
                message.WorkItemId,
                message.JobType,
                cancellationToken
            );

        if (associatedWorkItemJobCount == null)
        {
            logger.LogWarning("No a found for CustodianId: {CustodianId}.", message.CustodianId);
            return; // Is this the correct approach?
        }

        var workItemPayload = JsonSerializer.Deserialize<SearchWorkItemPayload>(
            associatedWorkItemJobCount.PayloadJson
        );

        foreach (var record in records)
        {
            var entry = new IdRegisterEntry
            {
                Sui = workItemPayload!.Sui,
                CustodianId = message.CustodianId,
                RecordType = record.RecordType,
                SystemId = record.SystemId,
                CustodianSubjectId = record.RecordId,
                Provenance = Provenance.AlreadyHeldByCustodian,
                LastIdDeliveredAtUtc = DateTimeOffset.UtcNow,
            };

            await idRegisterRepository.UpsertAsync(entry, cancellationToken);
        }

        // Apply PEP filtering
        logger.LogInformation("Applying PEP filtering to {Count} records", records.Count);

        var filteredRecords = ApplyPepFiltering(records);

        logger.LogInformation(
            "{AllowedCount} records allowed after PEP filtering",
            filteredRecords.Count
        );

        // Persist filtered records to SearchResultEntries
        var custodian = await custodianService.GetCustodianAsync(message.CustodianId);

        if (!custodian.Success || custodian.Value is null)
        {
            logger.LogWarning(
                "No custodian configuration found for CustodianId: {CustodianId}.",
                message.CustodianId
            );
        }

        foreach (var record in filteredRecords)
        {
            var entry = new SearchResultEntry
            {
                CustodianId = message.CustodianId,
                SearchingOrganisationId = "placeholder",
                CustodianName = custodian.Value?.OrgName ?? string.Empty,
                WorkItemId = message.WorkItemId,
                JobId = message.JobId,
                RecordId = record.Item.RecordId,
                RecordType = record.Item.RecordType,
                RecordUrl = record.Item.RecordUrl,
                SystemId = record.Item.SystemId,
                SubmittedAtUtc = message.SubmittedAtUtc,
            };

            await searchResultRepository.UpsertAsync(entry, cancellationToken);
        }

        // Mark job as completed
        await jobService.MarkCompletedAsync(message.JobId, cancellationToken);

        logger.LogInformation("Marked JobId: {JobId} as completed", message.JobId);
    }

    private IReadOnlyList<SearchResultWithDecision> ApplyPepFiltering(List<JobResultRecord> records)
    {
        // var pepInput = records
        //     .Select(r => new CustodianSearchResultItem
        //     {
        //         RecordId = r.RecordId,
        //         RecordType = r.RecordType,
        //         RecordUrl = r.RecordUrl,
        //         SystemId = r.SystemId
        //     })
        //     .ToList();

        // 2️⃣ Call policy service
        // var results = await pepFilteringService.FilterResultsAsync(
        //     sourceOrgId,
        //     destOrgId,
        //     destOrgType,
        //     pepInput,
        //     dsaPolicy,
        //     purpose,
        //     cancellationToken);

        return [];
    }
}

public record SearchWorkItemPayload
{
    public required string Sui { get; init; }
}
