using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;
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
        Summary = "Identifier of the search work item."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(SearchResultsV2),
        Description = "The status of a Find a Record search work item, and the results if available."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Description = "The requested search work item was not found."
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.Unauthorized,
        "application/json",
        typeof(Problem),
        Description = "Request was refused because it lacks valid authentication credentials."
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(Problem),
        Description = "The server encountered an unexpected condition that prevented it from fulfilling the request."
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
                ["RequestingOrganisationId"] = authContext.OrganisationId,
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                ["TraceParent"] = context.TraceContext.TraceParent,
            }
        );

        var result = await jobSearchService.GetSearchResultsAsync(
            workItemId,
            authContext.OrganisationId,
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
