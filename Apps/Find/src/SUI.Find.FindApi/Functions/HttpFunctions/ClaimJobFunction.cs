using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.OpenApi;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class ClaimJobFunction(ILogger<ClaimJobFunction> logger, IJobClaimService jobClaimService)
{
    [OpenApiOperation(
        operationId: "claim-job",
        tags: ["Work"],
        Summary = "Check if work is available, and atomically lease the job for it if so."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Created,
        contentType: "application/json",
        bodyType: typeof(JobInfo),
        Summary = "Information about the job that was leased."
    )]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Summary = "There are no jobs waiting to be worked on.",
        CustomHeaderType = typeof(RetryAfterHeader)
    )]
    [RequiredScopes("work-item.write")]
    [Function(nameof(ClaimJob))]
    public async Task<HttpResponseData> ClaimJob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/work/claim")]
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

        var submittingCustodianId = authContext.ClientId;

        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                { "SubmittingCustodianId", submittingCustodianId },
                { "TraceParent", context.TraceContext.TraceParent },
                { "TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty },
                { "InvocationId", context.InvocationId },
            }
        );

        logger.LogInformation(
            "Checking if job is available, and atomically leasing it if so, for custodian: {SubmittingCustodianId}",
            submittingCustodianId
        );

        var claimedJob = await jobClaimService.ClaimNextAvailableJobAsync(
            submittingCustodianId,
            cancellationToken
        );

        if (claimedJob != null)
        {
            return await HttpResponseUtility.CreatedResponse(req, claimedJob, cancellationToken);
        }

        return HttpResponseUtility.NoContentResponse(req).AddRetryAfterHeader();
    }
}
