using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public interface ISearchService
{
    Task<OneOf<SearchJobDto, Error>> StartSearchAsync(
        string inputPersonId,
        string clientId,
        DurableTaskClient client,
        string correlationId,
        CancellationToken cancellationToken
    );

    Task<OneOf<SearchJobDto, NotFound, Unauthorized, Error>> CancelSearchAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );

    Task<OneOf<SearchResultsDto, NotFound, Unauthorized, Error>> GetSearchResultsAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );

    Task<OneOf<SearchJobDto, Unauthorized, NotFound, Error>> GetSearchStatusAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );
}

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SearchService(
    ILogger<SearchService> logger,
    IPersonIdEncryptionService encryptionService,
    ICustodianService custodianService,
    IHashService hashService,
    IOptions<EncryptionConfiguration> encryptionConfig,
    ISearchResultEntryRepository searchResultEntryRepository
) : ISearchService
{
    public async Task<OneOf<SearchJobDto, Error>> StartSearchAsync(
        string inputPersonId,
        string clientId,
        DurableTaskClient client,
        string correlationId,
        CancellationToken cancellationToken
    )
    {
        var instanceId = $"{inputPersonId}-{clientId}";
        var hashedInstanceId = hashService.HmacSha256Hash(instanceId);

        var existingInstance = await client.GetInstanceAsync(
            hashedInstanceId,
            cancellation: cancellationToken
        );
        var hasExistingInstance =
            existingInstance is
            {
                RuntimeStatus: OrchestrationRuntimeStatus.Running
                    or OrchestrationRuntimeStatus.Pending
            };

        if (hasExistingInstance)
        {
            var originalJobId = existingInstance!.InstanceId;
            var jobStatus =
                existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running
                    ? SearchStatus.Running
                    : SearchStatus.Queued;

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Duplicate Search Request for existing JobId: {JobId} with Status: {Status}. Returning existing job.",
                    originalJobId,
                    jobStatus
                );

            var originalJob = new SearchJobDto
            {
                JobId = originalJobId,
                PersonId = inputPersonId,
                Status = jobStatus,
                CreatedAt = existingInstance.CreatedAt,
                LastUpdatedAt = existingInstance.LastUpdatedAt,
            };

            return originalJob;
        }

        var encryptDefinition = await custodianService.GetCustodianAsync(clientId);
        if (!encryptDefinition.Success || encryptDefinition.Value is null)
        {
            logger.LogWarning(
                "No custodian configuration found for ClientId: {ClientId}.",
                clientId
            );
            return new Error();
        }

        string personId;
        var encrypt = encryptionConfig.Value.EnablePersonIdEncryption;
        if (encryptDefinition.Value.Encryption is not null && encrypt)
        {
            var unencryptedPersonId = encryptionService.DecryptPersonIdToNhs(
                inputPersonId,
                encryptDefinition.Value.Encryption
            );

            if (!unencryptedPersonId.Success || unencryptedPersonId.Value is null)
            {
                logger.LogWarning("Failed to decrypt SUID for ClientId: {ClientId}.", clientId);
                return new Error();
            }

            personId = unencryptedPersonId.Value;
        }
        else
        {
            personId = inputPersonId;
        }

        var metaData = new SearchJobMetadata(inputPersonId, DateTime.UtcNow, correlationId);

        var policyContext = new PolicyContext(
            clientId,
            ApplicationConstants.PolicyEnforcement.Purpose,
            encryptDefinition.Value.OrgType
        );

        var orchestratorInput = new SearchOrchestratorInput(personId, metaData, policyContext);

        var jobId = await client.ScheduleNewOrchestrationInstanceAsync(
            "SearchOrchestrator",
            orchestratorInput,
            new StartOrchestrationOptions { InstanceId = hashedInstanceId },
            cancellationToken
        );

        var searchJob = new SearchJobDto
        {
            JobId = jobId,
            PersonId = inputPersonId,
            Status = SearchStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        return searchJob;
    }

    public async Task<OneOf<SearchJobDto, NotFound, Unauthorized, Error>> CancelSearchAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var metaData = await client.GetInstanceAsync(jobId, cancellation: cancellationToken);
            if (metaData is null)
            {
                return new NotFound();
            }

            // Check if the job belongs to the requesting client
            var input = ReadOrchestratorInput<SearchOrchestratorInput>(metaData);
            if (input is null || input.PolicyContext.ClientId != clientId)
            {
                return new Unauthorized();
            }

            var canCancel =
                metaData.RuntimeStatus
                is OrchestrationRuntimeStatus.Running
                    or OrchestrationRuntimeStatus.Pending;

            if (!canCancel)
            {
                logger.LogWarning(
                    "Cannot cancel search job {JobId} in status {Status}.",
                    jobId,
                    metaData.RuntimeStatus
                );
            }

            await client.TerminateInstanceAsync(jobId, "Cancelled by user", cancellationToken);

            // return success regardless of whether it could be cancelled. Keeps it idempotent.
            return new SearchJobDto
            {
                JobId = jobId,
                PersonId = input.Suid,
                Status = OrchestrationRuntimeStatus.Terminated.ToSuiSearchStatus(),
                CreatedAt = metaData.CreatedAt,
                LastUpdatedAt = metaData.LastUpdatedAt,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling search job {JobId}.", jobId);
            return new Error();
        }
    }

    public async Task<OneOf<SearchResultsDto, NotFound, Unauthorized, Error>> GetSearchResultsAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var metaData = await client.GetInstanceAsync(
                jobId,
                true,
                cancellation: cancellationToken
            );
            if (metaData is null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Search job with ID {JobId} not found.", jobId);
                return new NotFound();
            }

            var meta = ReadOrchestratorInput<SearchOrchestratorInput>(metaData);
            if (meta is null)
            {
                logger.LogWarning("Failed to read metadata for search job {JobId}.", jobId);
                return new Error();
            }

            if (meta.PolicyContext.ClientId != clientId)
            {
                logger.LogWarning(
                    "Unauthorized access attempt to search job {JobId} by client {ClientId}.",
                    jobId,
                    clientId
                );
                return new Unauthorized();
            }

            SearchResultItem[] finalItems;

            // If job is still running → return partial persisted results
            if (metaData.IsRunning)
            {
                var persistedItems = await searchResultEntryRepository.GetByWorkItemIdAsync(
                    jobId,
                    clientId,
                    cancellationToken
                );

                finalItems = persistedItems
                    .Where(r => r.SubmittedAtUtc >= metaData.CreatedAt)
                    .Select(r => new SearchResultItem
                    {
                        RecordType = r.RecordType,
                        RecordId = r.RecordId,
                        RecordUrl = r.RecordUrl,
                        SystemId = r.SystemId,
                        CustodianName = r.CustodianName,
                    })
                    .ToArray();
            }
            else
            {
                // Completed / Failed / Terminated → return orchestrator output (existing behaviour)
                if (!string.IsNullOrEmpty(metaData.SerializedOutput))
                {
                    finalItems =
                        JsonSerializer.Deserialize<SearchResultItem[]>(metaData.SerializedOutput)
                        ?? Array.Empty<SearchResultItem>();
                }
                else
                {
                    finalItems = Array.Empty<SearchResultItem>();
                }
            }

            return new SearchResultsDto
            {
                JobId = jobId,
                Suid = meta.Suid,
                Status = metaData.RuntimeStatus.ToSuiSearchStatus(),
                Items = finalItems,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving search results for JobId {JobId}. Error: {ErrorMessage}.",
                jobId,
                ex.Message
            );
            return new Error();
        }
    }

    public async Task<OneOf<SearchJobDto, Unauthorized, NotFound, Error>> GetSearchStatusAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var jobStatus = await client.GetInstanceAsync(jobId, true, cancellationToken);
            if (jobStatus is null)
            {
                return new NotFound();
            }

            var input = ReadOrchestratorInput<SearchOrchestratorInput>(jobStatus);
            if (input is null || input.PolicyContext.ClientId != clientId)
            {
                return new Unauthorized();
            }

            var dto = new SearchJobDto
            {
                JobId = jobId,
                PersonId = input.Suid,
                Status = jobStatus.RuntimeStatus.ToSuiSearchStatus(),
                CreatedAt = jobStatus.CreatedAt,
                LastUpdatedAt = jobStatus.LastUpdatedAt,
            };

            return dto;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving search job status for JobId {JobId}. Error: {ErrorMessage}.",
                jobId,
                ex.Message
            );
            return new Error();
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Sealed method that is hard to mock in unit tests")]
    // Made virtual to allow mocking in unit tests
    public virtual T? ReadOrchestratorInput<T>(OrchestrationMetadata jobStatus)
    {
        return jobStatus.ReadInputAs<T>();
    }
}

public enum CancelSearchResult
{
    NotFound,
    Canceled,
    Failed,
    CannotCancel,
    Unauthorized,
}
