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

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class FetchRecordFunction(ILogger<FetchRecordFunction> logger, IFetchRecordService fetchRecordService)
{
    [Function(nameof(FetchRecord))]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "fetchRecord",
        tags: ["Fetch"],
        Summary = "Fetch a record from a participating system")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(RecordBase))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.BadGateway, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> FetchRecord(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/records/{recordId}")]
        HttpRequestData req,
        string recordId,
        FunctionContext context,
        CancellationToken cancellationToken)
    {

        using var logScope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = context.InvocationId }
        );


        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            || authObj is not AuthContext authContext
        )
        {
            logger.LogError("Unauthorised request to fetch record.");
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "",
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
                "Bad or missing recordId parameter",
                cancellationToken
            );
        }

        var result = await fetchRecordService.FetchRecordAsync(
            recordId,
            authContext.ClientId,
            cancellationToken
        );

        if (!result.Success)
        {
            if (result.Error == "NotFound")
            {
                logger.LogInformation("Record not found: {RecordId}.", recordId);
                return await HttpResponseUtility.NotFoundResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                );
            }

            logger.LogInformation("Error searching for record: {RecordId}.", recordId);
            return await HttpResponseUtility.InternalServerErrorResponse(
                req,
                context.InvocationId,
                cancellationToken
            );

        }

        return await HttpResponseUtility.OkResponse(req, result.Value, cancellationToken);

    }
}