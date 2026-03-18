using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.JobRepository;
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

            await jobService.MarkCompletedAsync(
                message.JobId,
                message.CustodianId,
                cancellationToken
            );
            return;
        }

        var records = message.Records;

        if (records.Count == 0)
        {
            logger.LogInformation("No records submitted for JobId {JobId}", message.JobId);

            await jobService.MarkCompletedAsync(
                message.JobId,
                message.CustodianId,
                cancellationToken
            );
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

        var job = await jobService.GetJobByIdAndCustodianIdAsync(
            message.JobId,
            message.CustodianId,
            cancellationToken
        );

        if (job is null)
        {
            logger.LogWarning("No job matching found for JobId: {JobId}.", message.JobId);
        }

        var custodian = await custodianService.GetCustodianAsync(message.CustodianId);

        if (!custodian.Success || custodian.Value is null)
        {
            logger.LogWarning(
                "No custodian configuration found for CustodianId: {CustodianId}.",
                message.CustodianId
            );
        }

        var searchingOrganisation = await custodianService.GetCustodianAsync(
            job?.SearchingOrganisationId!
        );

        if (!custodian.Success || custodian.Value is null)
        {
            logger.LogWarning(
                "No custodian configuration found for SearchingOrganisationId: {SearchingOrganisationId}.",
                job?.SearchingOrganisationId
            );
        }

        // Apply PEP filtering
        logger.LogInformation("Applying PEP filtering to {Count} records", records.Count);

        var filteredRecords = await ApplyPepFiltering(
            records,
            custodian.Value!,
            searchingOrganisation.Value!,
            cancellationToken
        );

        logger.LogInformation(
            "{AllowedCount} records allowed after PEP filtering",
            filteredRecords.Count
        );

        // Persist filtered records to SearchResultEntries
        foreach (var record in filteredRecords)
        {
            var entry = new SearchResultEntry
            {
                CustodianId = message.CustodianId,
                SearchingOrganisationId = job?.SearchingOrganisationId ?? string.Empty,
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
        await jobService.MarkCompletedAsync(message.JobId, message.CustodianId, cancellationToken);

        logger.LogInformation("Marked JobId: {JobId} as completed", message.JobId);
    }

    private async Task<IReadOnlyList<SearchResultWithDecision>> ApplyPepFiltering(
        List<JobResultRecord> records,
        ProviderDefinition custodian,
        ProviderDefinition searchingOrganisation,
        CancellationToken cancellationToken
    )
    {
        var pepInput = records
            .Select(r => new CustodianSearchResultItem(
                custodian.OrgId,
                r.RecordType,
                r.RecordUrl,
                r.SystemId,
                custodian.OrgName,
                r.RecordId
            ))
            .ToList();

        var results = await pepFilteringService.FilterResultsAsync(
            custodian.OrgId,
            searchingOrganisation.OrgId,
            searchingOrganisation.OrgType,
            pepInput,
            custodian.DsaPolicy,
            "SAFEGUARDING",
            cancellationToken
        );

        return results;
    }
}
