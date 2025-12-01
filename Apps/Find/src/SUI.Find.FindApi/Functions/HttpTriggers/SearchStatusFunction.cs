using System.Diagnostics.CodeAnalysis;
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

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SearchStatusFunction(
    ILogger<SearchStatusFunction> logger,
    ISearchService searchService
)
{
    #region OpenAPI
    [OpenApiOperation(
        operationId: "getSearch",
        tags: ["Searches"],
        Summary = "Get search status",
        Description = "Retrieve the current status of a Find a Record search job."
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Identifier of the search job.",
        Description = "The unique identifier for the search job."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(SearchJob),
        Summary = "Current search job status.",
        Description = "Returns the search job details if found."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Search job not found.",
        Description = "Returned when the search job does not exist."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Error",
        Description = "Error response."
    )]
    #endregion
    [RequiredScopes("find-record.read")]
    [Function(nameof(SearchJobTrigger))]
    public async Task<HttpResponseData> SearchJobTrigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/searches/{jobId}")]
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

        logger.LogInformation("Requesting Search Job Status for Id: {JobId}", jobId);
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
        var jobStatus = await searchService.GetSearchStatusAsync(
            jobId,
            authContext.ClientId,
            client,
            cancellationToken
        );

        return jobStatus switch
        {
            SearchJobResult.Success s => await CreateSuccessResponse(req, s.Job, cancellationToken),
            SearchJobResult.NotFound => await HttpResponseUtility.NotFoundResponse(
                req,
                context.InvocationId,
                cancellationToken
            ),
            SearchJobResult.Unauthorized => await HttpResponseUtility.UnauthorizedResponse(
                req,
                context.InvocationId,
                cancellationToken
            ),
            _ => await HttpResponseUtility.InternalServerErrorResponse(
                req,
                context.InvocationId,
                cancellationToken
            ),
        };
    }

    private static async Task<HttpResponseData> CreateSuccessResponse(
        HttpRequestData req,
        SearchJobDto result,
        CancellationToken cancellationToken
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        var searchResults = SearchJob.FromDto(result);
        await response.WriteAsJsonAsync(searchResults, cancellationToken);
        return response;
    }
}
