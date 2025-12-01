using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

public interface ISearchService
{
    Task<CancelSearchDto> CancelSearchAsync(
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
