using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class SearchResultsV2Function(
    ILogger<SearchResultsV2Function> logger,
    IJobSearchService jobSearchService
)
{
    #region OpenApi

    [OpenApiOperation(
        operationId: "getSearchResultsV2",
        tags: ["SearchesV2"],
        Summary = "Get search results",
        Description = "Returns the status for a Find a Record search work item, and the results if available."
    )]
    [OpenApiParameter(
        name: "workItemId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Identifier of the search work item.",
        Description = "Identifier of the search work item."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(SearchResultsV2),
        Summary = "Results for the specified search work item."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Search work item not found."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Error"
    )]
    #endregion

    [RequiredScopes("find-record.read")]
    [Function(nameof(SearchResultsV2Trigger))]
    public async Task<HttpResponseData> SearchResultsV2Trigger(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "v2/searches/{workItemId}/results"
        )]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string workItemId,
        CancellationToken cancellationToken
    )
    {
        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            || authObj is not AuthContext authContext
        )
        {
            return await HttpResponseUtility.UnauthorizedResponse(
                req,
                context.InvocationId,
                cancellationToken
            );
        }

        using var logScope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["InvocationId"] = context.InvocationId,
                ["WorkItemId"] = workItemId,
                ["RequestingOrganisationId"] = authContext.ClientId,
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                ["TraceParent"] = context.TraceContext.TraceParent,
            }
        );

        var result = await jobSearchService.GetSearchResultsAsync(
            workItemId,
            authContext.ClientId,
            cancellationToken
        );

        return await result.Match(
            async searchResult =>
                await HttpResponseUtility.OkResponse(
                    req,
                    SearchResultsV2.FromDto(searchResult),
                    cancellationToken
                ),
            async notFound =>
                await HttpResponseUtility.NotFoundResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                ),
            async forbidden =>
                await HttpResponseUtility.ForbiddenResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                ),
            async error =>
                await HttpResponseUtility.InternalServerErrorResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                )
        );
    }
}
