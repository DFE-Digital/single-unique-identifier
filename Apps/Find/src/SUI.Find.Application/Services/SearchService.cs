using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Services;

public interface ISearchService
{
    Task<SearchCancelResult> CancelSearchAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );

    Task<SearchResultsDto> GetSearchResultsAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );

    Task<SearchJobResult> GetSearchStatusAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );
}

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SearchService(ILogger<SearchService> logger) : ISearchService
{
    public async Task<SearchCancelResult> CancelSearchAsync(
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
                return new SearchCancelResult.NotFound();
            }

            // Check if the job belongs to the requesting client
            var input = ReadOrchestratorInput<SearchOrchestratorInput>(metaData);
            if (input is null || input.PolicyContext.ClientId != clientId)
            {
                return new SearchCancelResult.Unauthorized();
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
            return new SearchCancelResult.Success(
                new SearchJobDto
                {
                    JobId = jobId,
                    Suid = input.Suid,
                    Status = OrchestrationRuntimeStatus.Terminated.ToSuiSearchStatus(),
                    CreatedAt = metaData.CreatedAt,
                    LastUpdatedAt = metaData.LastUpdatedAt,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling search job {JobId}.", jobId);
            return new SearchCancelResult.Error();
        }
    }

    public async Task<SearchResultsDto> GetSearchResultsAsync(
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
                logger.LogInformation("Search job with ID {JobId} not found.", jobId);
                return SearchResultsDto.NotFound(jobId);
            }

            var meta = ReadOrchestratorInput<SearchOrchestratorInput>(metaData);
            if (meta is null)
            {
                return SearchResultsDto.Error(jobId, "Failed to read search job metadata.");
            }

            if (meta.PolicyContext.ClientId != clientId)
            {
                logger.LogWarning(
                    "Unauthorized access attempt to search job {JobId} by client {ClientId}.",
                    jobId,
                    clientId
                );
                return SearchResultsDto.Unauthorized(jobId);
            }

            if (metaData.IsRunning)
            {
                return SearchResultsDto.Success(
                    jobId,
                    meta.Suid,
                    metaData.RuntimeStatus.ToSuiSearchStatus(),
                    []
                );
            }

            if (string.IsNullOrEmpty(metaData.SerializedOutput))
            {
                return SearchResultsDto.Success(
                    jobId,
                    meta.Suid,
                    metaData.RuntimeStatus.ToSuiSearchStatus(),
                    [] // No results found
                );
            }
            var items = JsonSerializer.Deserialize<SearchResultItem[]>(metaData.SerializedOutput);

            return SearchResultsDto.Success(
                jobId,
                meta.Suid,
                metaData.RuntimeStatus.ToSuiSearchStatus(),
                items is { Length: > 0 } ? items : []
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving search results for JobId {JobId}. Error: {ErrorMessage}.",
                jobId,
                ex.Message
            );
            return SearchResultsDto.Error(
                jobId,
                "An error occurred while retrieving search results."
            );
        }
    }

    public async Task<SearchJobResult> GetSearchStatusAsync(
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
                return new SearchJobResult.NotFound();
            }

            var input = ReadOrchestratorInput<SearchOrchestratorInput>(jobStatus);
            if (input is null || input.PolicyContext.ClientId != clientId)
            {
                return new SearchJobResult.Unauthorized();
            }

            var dto = new SearchJobDto
            {
                JobId = jobId,
                Suid = input.Suid,
                Status = jobStatus.RuntimeStatus.ToSuiSearchStatus(),
                CreatedAt = jobStatus.CreatedAt,
                LastUpdatedAt = jobStatus.LastUpdatedAt,
            };

            var searchJob = new SearchJobResult.Success(dto);

            return searchJob;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving search job status for JobId {JobId}. Error: {ErrorMessage}.",
                jobId,
                ex.Message
            );
            return new SearchJobResult.Failed();
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
