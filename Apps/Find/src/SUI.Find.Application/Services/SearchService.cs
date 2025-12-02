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
    Task<CancelSearchDto> CancelSearchAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );

    Task<SearchResult> GetSearchResultsAsync(
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
    public async Task<CancelSearchDto> CancelSearchAsync(
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
                return new CancelSearchDto(CancelSearchResult.NotFound, "No search job found.");
            }

            // Check if the job belongs to the requesting client
            var input = ReadOrchestratorInput<SearchOrchestratorInput>(metaData);
            if (input is null || input.PolicyContext.ClientId != clientId)
            {
                return new CancelSearchDto(CancelSearchResult.Unauthorized, "Unauthorized");
            }

            var canCancel =
                metaData.RuntimeStatus
                is OrchestrationRuntimeStatus.Running
                    or OrchestrationRuntimeStatus.Pending;

            if (!canCancel)
            {
                var state = metaData.RuntimeStatus.ToString();
                return new CancelSearchDto(
                    CancelSearchResult.CannotCancel,
                    $"Search job cannot be canceled in its current state: {state}"
                );
            }

            await client.TerminateInstanceAsync(jobId, "Cancelled by user", cancellationToken);
            return new CancelSearchDto(CancelSearchResult.Canceled, string.Empty);
        }
        catch
        {
            return new CancelSearchDto(
                CancelSearchResult.Failed,
                "Failed to cancel the search job."
            );
        }
    }

    public async Task<SearchResult> GetSearchResultsAsync(
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
                return new SearchResult.NotFound();
            }

            var meta = ReadOrchestratorInput<SearchOrchestratorInput>(metaData);
            if (meta is null)
            {
                logger.LogWarning("Failed to read metadata for search job {JobId}.", jobId);
                return new SearchResult.Failed();
            }

            if (meta.PolicyContext.ClientId != clientId)
            {
                logger.LogWarning(
                    "Unauthorized access attempt to search job {JobId} by client {ClientId}.",
                    jobId,
                    clientId
                );
                return new SearchResult.Unauthorized();
            }

            if (metaData.IsRunning)
            {
                return new SearchResult.Success(
                    new SearchResultsDto
                    {
                        JobId = jobId,
                        Suid = meta.Suid,
                        Status = metaData.RuntimeStatus.ToSuiSearchStatus(),
                        Items = [],
                    }
                );
            }

            if (string.IsNullOrEmpty(metaData.SerializedOutput))
            {
                return new SearchResult.Success(
                    new SearchResultsDto
                    {
                        JobId = jobId,
                        Suid = meta.Suid,
                        Status = metaData.RuntimeStatus.ToSuiSearchStatus(),
                        Items = [],
                    }
                );
            }
            var items = JsonSerializer.Deserialize<SearchResultItem[]>(metaData.SerializedOutput);

            return new SearchResult.Success(
                new SearchResultsDto
                {
                    JobId = jobId,
                    Suid = meta.Suid,
                    Status = metaData.RuntimeStatus.ToSuiSearchStatus(),
                    Items = items ?? [],
                }
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
            return new SearchResult.Failed();
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
