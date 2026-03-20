using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class WorkAvailableFunction(
    ILogger<WorkAvailableFunction> logger,
    IJobClaimService jobClaimService
)
{
    [OpenApiOperation(
        operationId: "work-available",
        tags: ["Searches"],
        Summary = "Check if work is available"
    )]
    [RequiredScopes("work-item.read")]
    [Function(nameof(WorkAvailable))]
    public async Task<HttpResponseData> WorkAvailable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "head", Route = "v2/work/available")]
            HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken
    )
    {
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

        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                { "SubmittingCustodianId", authContext.ClientId },
                { "TraceParent", context.TraceContext.TraceParent },
                { "TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty },
                { "InvocationId", context.InvocationId },
            }
        );

        logger.LogInformation(
            "Checking if work is available for custodian: {id}",
            authContext.ClientId
        );

        var result = await jobClaimService.DoesCustodianHaveJobs(
            authContext.ClientId,
            cancellationToken
        );

        return CreateSuccessResponse(req, result);
    }

    private static HttpResponseData CreateSuccessResponse(HttpRequestData req, bool hasWork)
    {
        var response = req.CreateResponse(hasWork ? HttpStatusCode.OK : HttpStatusCode.NoContent);
        response.Headers.Add("Cache-Control", "no-store, no-cache, max-age=0, must-revalidate");
        response.Headers.Add("Pragma", "no-cache");
        response.Headers.Add("Expires", DateTime.MinValue.ToUniversalTime().ToString("R"));
        response.Headers.Add("Vary", "Authorization");
        return response;
    }
}
