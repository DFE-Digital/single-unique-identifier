using System.Diagnostics.CodeAnalysis;
using Microsoft.DurableTask.Client;
using SUI.Find.Application.Dtos;

namespace SUI.Find.Application.Services;

public interface ISearchService
{
    Task<CancelSearchDto> CancelSearchAsync(
        string jobId,
        string clientId,
        DurableTaskClient client,
        CancellationToken cancellationToken
    );
}

public class SearchService : ISearchService
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
            // (Assuming clientId is stored in the input of the orchestration)
            var input = ReadOrchestratorInput<SearchJobOrchestrationInput>(metaData);
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
