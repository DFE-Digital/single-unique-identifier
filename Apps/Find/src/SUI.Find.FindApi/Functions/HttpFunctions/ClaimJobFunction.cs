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
        Summary = "There are no jobs waiting to be worked on."
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

        return await CreateSuccessResponseAsync(req, claimedJob, cancellationToken);
    }

    private static async Task<HttpResponseData> CreateSuccessResponseAsync(
        HttpRequestData req,
        JobInfo? claimedJob,
        CancellationToken cancellationToken
    )
    {
        var response = req.CreateResponse(
            claimedJob != null ? HttpStatusCode.Created : HttpStatusCode.NoContent
        );

        response.Headers.Add("Cache-Control", "no-store, no-cache, max-age=0, must-revalidate");
        response.Headers.Add("Pragma", "no-cache");
        response.Headers.Add("Expires", DateTime.MinValue.ToUniversalTime().ToString("R"));
        response.Headers.Add("Vary", "Authorization");

        if (claimedJob != null)
        {
            await response.WriteAsJsonAsync(claimedJob, cancellationToken);
        }

        return response;
    }
}
