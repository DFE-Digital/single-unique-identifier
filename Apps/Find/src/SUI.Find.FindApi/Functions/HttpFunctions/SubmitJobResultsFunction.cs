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
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class SubmitJobResultsFunction(
    ILogger<SubmitJobResultsFunction> logger,
    IJobProcessorService jobService,
    IJobResultsQueueClient queueClient
)
{
    [Function(nameof(SubmitJobResults))]
    [RequiredScopes("work-item.write")]
    [OpenApiOperation(
        operationId: "SubmitJobResults",
        tags: ["WorkItems"],
        Summary = "Submit results for a leased work item"
    )]
    [OpenApiRequestBody("application/json", typeof(SubmitJobResultsRequest), Required = true)]
    [OpenApiResponseWithoutBody(HttpStatusCode.Accepted)]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> SubmitJobResults(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/work/result")]
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
            return await HttpResponseUtility.UnauthorizedResponse(
                req,
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

        var custodianId = authContext.ClientId;

        var job = await jobService.ValidateLeaseAsync(
            request.JobId,
            request.LeaseId,
            custodianId,
            cancellationToken
        );

        if (job is null)
        {
            return await HttpResponseUtility.BadRequestResponse(
                req,
                context.InvocationId,
                "Invalid lease or job state",
                "Lease validation failed",
                cancellationToken
            );
        }

        // if (job.CompletedAtUtc.HasValue)
        // {
        //     return await HttpResponseUtility.ProblemResponse(
        //         req,
        //         HttpStatusCode.Conflict,
        //         "Invalid request",
        //         "Job is already completed",
        //         context.InvocationId,
        //         cancellationToken
        //     );
        // }

        using var logScope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["LeaseId"] = request.LeaseId,
                ["JobId"] = job.JobId,
                ["SubmittingCustodianId"] = custodianId,
                ["TraceParent"] = context.TraceContext.TraceParent,
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                ["InvocationId"] = context.InvocationId,
            }
        );

        var queueMessage = new JobResultMessage
        {
            JobId = job.JobId,
            WorkItemId = job.WorkItemId ?? string.Empty,
            LeaseId = request.LeaseId,
            CustodianId = custodianId,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            JobType = job.JobType,
            Records = request.Records,
        };

        await queueClient.SendAsync(queueMessage, cancellationToken);

        var response = req.CreateResponse(HttpStatusCode.Accepted);

        AddNoCacheHeaders(response);

        return response;
    }

    private bool TryGetRequestModel(
        HttpRequestData req,
        [NotNullWhen(true)] out SubmitJobResultsRequest? model
    )
    {
        model = null;

        try
        {
            var requestBody = req.ReadAsString();

            var request = JsonSerializer.Deserialize<SubmitJobResultsRequest>(
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
            logger.LogError(ex, "Failed to parse SubmitJobResults request: {Message}", ex.Message);
            return false;
        }
    }

    private static void AddNoCacheHeaders(HttpResponseData response)
    {
        response.Headers.Add("Cache-Control", "no-store, no-cache, max-age=0, must-revalidate");
        response.Headers.Add("Pragma", "no-cache");
        response.Headers.Add("Expires", "0");
        response.Headers.Add("Vary", "Authorization");
    }
}
