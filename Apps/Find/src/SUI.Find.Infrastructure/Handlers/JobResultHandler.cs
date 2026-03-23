using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
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
    IPolicyEnforcementService pepService,
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

        // JobType check
        if (message.JobType != JobType.CustodianLookup)
        {
            logger.LogInformation("Skipping unsupported JobType {JobType}", message.JobType);
            await jobService.MarkCompletedAsync(
                message.JobId,
                message.CustodianId,
                cancellationToken
            );
            return;
        }

        if (message.Records.Count == 0)
        {
            logger.LogInformation("No records submitted for JobId {JobId}", message.JobId);
            await jobService.MarkCompletedAsync(
                message.JobId,
                message.CustodianId,
                cancellationToken
            );
            return;
        }

        var payload = await GetWorkItemPayload(message, cancellationToken);
        if (payload is null)
        {
            return;
        }

        await UpsertIdRegister(message, payload, cancellationToken);

        var context = await BuildContext(message, cancellationToken);
        if (context is null)
        {
            return;
        }

        var filtered = await ApplyPepFiltering(message.Records, context, cancellationToken);

        await PersistSearchResults(filtered, message, context, cancellationToken);

        await jobService.MarkCompletedAsync(message.JobId, message.CustodianId, cancellationToken);

        logger.LogInformation("Marked JobId {JobId} as completed", message.JobId);
    }

    // WorkItem Payload
    private async Task<SearchWorkItemPayload?> GetWorkItemPayload(
        JobResultMessage message,
        CancellationToken cancellationToken
    )
    {
        var jobCount = await workItemJobCountRepository.GetByWorkItemIdAndJobTypeAsync(
            message.WorkItemId,
            message.JobType,
            message.CustodianId,
            cancellationToken
        );

        if (jobCount is null)
        {
            logger.LogWarning(
                "No WorkItemJobCount found for WorkItemId {WorkItemId}",
                message.WorkItemId
            );
            return null;
        }

        return JsonSerializer.Deserialize<SearchWorkItemPayload>(jobCount.PayloadJson);
    }

    // ID Register
    private async Task UpsertIdRegister(
        JobResultMessage message,
        SearchWorkItemPayload payload,
        CancellationToken cancellationToken
    )
    {
        var records = message.Records;

        logger.LogInformation("Upserting {Count} records into ID Register", records.Count);

        foreach (var record in records)
        {
            var entry = new IdRegisterEntry
            {
                Sui = payload.Sui,
                CustodianId = message.CustodianId,
                RecordType = record.RecordType,
                SystemId = record.SystemId,
                CustodianSubjectId = record.RecordId,
                Provenance = Provenance.AlreadyHeldByCustodian,
                LastIdDeliveredAtUtc = DateTimeOffset.UtcNow,
            };

            await idRegisterRepository.UpsertAsync(entry, cancellationToken);
        }
    }

    // Context (Job + Custodians)
    private async Task<PepContext?> BuildContext(
        JobResultMessage message,
        CancellationToken cancellationToken
    )
    {
        var job = await jobService.GetJobByIdAndCustodianIdAsync(
            message.JobId,
            message.CustodianId,
            cancellationToken
        );

        if (job is null)
        {
            logger.LogWarning("Job not found for JobId {JobId}", message.JobId);
            return null;
        }

        var custodian = await custodianService.GetCustodianAsync(message.CustodianId);
        if (!custodian.Success || custodian.Value is null)
        {
            logger.LogWarning("Custodian config not found for {CustodianId}", message.CustodianId);
            return null;
        }

        var searchingOrg = await custodianService.GetCustodianAsync(job.SearchingOrganisationId);
        if (!searchingOrg.Success || searchingOrg.Value is null)
        {
            logger.LogWarning(
                "Searching organisation config not found for {SearchingOrganisationId}",
                job.SearchingOrganisationId
            );
            return null;
        }

        return new PepContext(custodian.Value, searchingOrg.Value, job.SearchingOrganisationId);
    }

    // PEP Filtering
    private async Task<IReadOnlyList<SearchResultWithDecision>> ApplyPepFiltering(
        List<JobResultRecord> records,
        PepContext context,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Applying PEP filtering to {Count} records", records.Count);

        var input = records
            .Select(r => new CustodianSearchResultItem(
                context.Custodian.OrgId,
                r.RecordType,
                r.RecordUrl,
                r.SystemId,
                context.Custodian.OrgName,
                r.RecordId
            ))
            .ToList();

        var resultsWithDecision = await pepService.FilterResultsAsync(
            context.Custodian.OrgId,
            context.SearchingOrganisation.OrgId,
            context.SearchingOrganisation.OrgType,
            input,
            context.Custodian.DsaPolicy,
            ApplicationConstants.PolicyEnforcementPurposes.Safeguarding,
            cancellationToken
        );

        var results = resultsWithDecision.Where(r => r.Decision.IsAllowed).ToImmutableList();

        logger.LogInformation("{AllowedCount} records allowed after PEP filtering", results.Count);

        return results;
    }

    // Persistence
    private async Task PersistSearchResults(
        IReadOnlyList<SearchResultWithDecision> results,
        JobResultMessage message,
        PepContext context,
        CancellationToken cancellationToken
    )
    {
        foreach (var item in results.Select(r => r.Item))
        {
            var entry = new SearchResultEntry
            {
                CustodianId = message.CustodianId,
                SearchingOrganisationId = context.SearchingOrganisationId,
                CustodianName = context.Custodian.OrgName,
                WorkItemId = message.WorkItemId,
                JobId = message.JobId,
                RecordId = item.RecordId,
                RecordType = item.RecordType,
                RecordUrl = item.RecordUrl,
                SystemId = item.SystemId,
                SubmittedAtUtc = message.SubmittedAtUtc,
            };

            await searchResultRepository.UpsertAsync(entry, cancellationToken);
        }
    }

    private sealed record PepContext(
        ProviderDefinition Custodian,
        ProviderDefinition SearchingOrganisation,
        string SearchingOrganisationId
    );
}
