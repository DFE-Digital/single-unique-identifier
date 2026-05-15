using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class CancelSearchFunction(
    ILogger<CancelSearchFunction> logger,
    ISearchService searchService
)
{
    #region OpenApi
    [OpenApiOperation(
        operationId: "cancelSearch",
        tags: ["Searches"],
        Summary = "Cancel a search job"
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Job ID",
        Description = "The unique identifier for the search job."
    )]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Summary = "Search job cancelled"
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Search job not found"
    )]
    #endregion
    [RequiredScopes("find-record.write")]
    [Function(nameof(CancelSearch))]
    public async Task<HttpResponseData> CancelSearch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "v1/searches/{jobId}")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string jobId,
        FunctionContext context,
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

        var result = await searchService.CancelSearchAsync(
            jobId,
            authContext.ClientId,
            client,
            cancellationToken
        );
        return await result.Match(
            async job =>
                await HttpResponseUtility.AcceptedResponse(
                    req,
                    SearchJob.FromDto(job),
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
