using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class SearchResultsFunction(
    ILogger<SearchResultsFunction> logger,
    ISearchService searchService
)
{
    #region OpenApi
    [OpenApiOperation(
        operationId: "getSearchResults",
        tags: new[] { "Searches" },
        Summary = "Get search results",
        Description = "Returns the results for a Find a Record search job. Results MAY be available before the job reaches `completed` status, depending on implementation."
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Identifier of the search job.",
        Description = "Identifier of the search job."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(SearchResults),
        Summary = "Results for the specified search job."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem), // Replace with your actual model
        Summary = "Search job not found."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(Problem), // Replace with your actual model
        Summary = "Error"
    )]
    #endregion
    [RequiredScopes("find-record.read")]
    [Function(nameof(SearchResultsTrigger))]
    public async Task<HttpResponseData> SearchResultsTrigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/searches/{jobId}/results")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string jobId,
        CancellationToken cancellationToken
    )
    {
        using var logScope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = context.InvocationId }
        );

        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            || authObj is not AuthContext authContext
        )
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "",
                context.InvocationId,
                cancellationToken
            );
        }

        var result = await searchService.GetSearchResultsAsync(
            jobId,
            authContext.ClientId,
            client,
            cancellationToken
        );

        return result.ResultsStatus switch
        {
            SearchResultsStatus.Success => await CreateSuccessResponse(
                req,
                result,
                cancellationToken
            ),
            SearchResultsStatus.NotFound => await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.NotFound,
                "Not Found",
                $"Search job with ID {jobId} not found.",
                context.InvocationId,
                cancellationToken
            ),
            SearchResultsStatus.Unauthorized => await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You do not have permission to access this search job.",
                context.InvocationId,
                cancellationToken
            ),
            _ => await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                result.ErrorMessage ?? "An error occurred while retrieving search results.",
                context.InvocationId,
                cancellationToken
            ),
        };
    }

    private static async Task<HttpResponseData> CreateSuccessResponse(
        HttpRequestData req,
        SearchResultsDto result,
        CancellationToken cancellationToken
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        var searchResults = SearchResults.FromDto(result);
        await response.WriteAsJsonAsync(searchResults, cancellationToken);
        return response;
    }
}
