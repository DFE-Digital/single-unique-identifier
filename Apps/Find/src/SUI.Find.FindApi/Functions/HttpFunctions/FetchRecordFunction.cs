using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class FetchRecordFunction(
    ILogger<FetchRecordFunction> logger,
    IFetchRecordService fetchRecordService
)
{
    [Function(nameof(FetchRecord))]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "fetchRecord",
        tags: ["Fetch"],
        Summary = "Fetch a record from a participating system"
    )]
    [OpenApiParameter(
        "recordId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string)
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CustodianRecord))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.BadGateway, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> FetchRecord(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/records/{recordId}")]
            HttpRequestData req,
        string recordId,
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
            logger.LogDebug("FAILED AUTH CONTEXT CHECK");
            return await HttpResponseUtility.UnauthorizedResponse(
                req,
                context.InvocationId,
                cancellationToken
            );
        }

        if (string.IsNullOrWhiteSpace(recordId))
        {
            logger.LogError("Invalid Record Id");
            return await HttpResponseUtility.BadRequestResponse(
                req,
                context.InvocationId,
                "Invalid record Id",
                cancellationToken: cancellationToken
            );
        }

        var result = await fetchRecordService.FetchRecordAsync(
            recordId,
            authContext.ClientId,
            cancellationToken
        );

        return await result.Match(
            async record => await HttpResponseUtility.OkResponse(req, record, cancellationToken),
            async notFound =>
                await HttpResponseUtility.NotFoundResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                ),
            async unauthorized =>
            {
                logger.LogDebug("FAILED ON FETCH RECORD AUTHORIZATION");
                return await HttpResponseUtility.UnauthorizedResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                );
            },
            async error =>
                await HttpResponseUtility.InternalServerErrorResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                )
        );
    }
}
