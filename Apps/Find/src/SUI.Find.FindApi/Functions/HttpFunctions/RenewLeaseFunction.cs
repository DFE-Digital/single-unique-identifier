using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class RenewLeaseFunction(
    ILogger<RenewLeaseFunction> logger,
    IJobClaimService jobClaimService
)
{
    [OpenApiOperation(
        operationId: "renew-lease",
        tags: ["Work"],
        Summary = "Renew the lease on a job"
    )]
    [RequiredScopes("work-item.write")]
    [Function(nameof(RenewLease))]
    public async Task<HttpResponseData> RenewLease(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/work/lease/renew")]
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

        if (!TryGetRequestModel(req, out var request))
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "The request body is missing or malformed.",
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
                { "JobId", request.JobId },
                { "LeaseId", request.LeaseId },
            }
        );

        logger.LogInformation(
            "Extending lease for job: {JobId} with lease: {LeaseId}",
            request.JobId,
            request.LeaseId
        );

        var jobInfo = await jobClaimService.ExtendJobLeaseAsync(
            authContext.ClientId,
            request.JobId,
            request.LeaseId,
            cancellationToken
        );

        if (jobInfo == null)
        {
            return HttpResponseUtility.NoContentResponse(req);
        }

        var result = new RenewJobLeaseResponse
        {
            JobId = jobInfo.JobId,
            LeaseId = jobInfo.LeaseId,
            WorkItemId = jobInfo.WorkItemId ?? "",
            LeaseExpiresUtc = jobInfo.LeaseExpiresAtUtc,
        };

        return await HttpResponseUtility.OkResponse(req, result, cancellationToken);
    }

    private bool TryGetRequestModel(
        HttpRequestData req,
        [NotNullWhen(true)] out RenewJobLeaseRequest? model
    )
    {
        model = null;

        try
        {
            var requestBody = req.ReadAsString();

            var request = JsonSerializer.Deserialize<RenewJobLeaseRequest>(
                requestBody!,
                JsonSerializerOptions.Web
            );

            if (request is null)
            {
                return false;
            }

            model = request;
            return true;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse RenewJobLease request: {Message}", ex.Message);
            return false;
        }
    }
}
