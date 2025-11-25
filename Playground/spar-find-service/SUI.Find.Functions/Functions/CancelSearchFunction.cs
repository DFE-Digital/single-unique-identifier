using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Microsoft.DurableTask.Client;
using Models;
using Interfaces;

namespace Functions;

public sealed class CancelSearchFunction(IFindOrchestrationService orchestration)
{
    private readonly IFindOrchestrationService _orchestration = orchestration;

    [Function("CancelSearch")]
    [RequiredScopes("find-record.write")]
    [OpenApiOperation(operationId: "cancelSearch", tags: new[] { "Find" }, Summary = "Cancel a search")]
    [OpenApiParameter("jobId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.Accepted, "application/json", typeof(SearchJob))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/searches/{jobId}")] HttpRequestData req,
        string jobId,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        if (!context.Items.TryGetValue("AuthContext", out var authObj) || authObj is not AuthContext auth)
        {
            return await ProblemResponse(req, HttpStatusCode.Unauthorized, "Unauthorised", "Missing or invalid bearer token.");
        }

        try
        {
            var job = await _orchestration.CancelAsync(durableClient, auth, jobId, context.CancellationToken);

            var res = req.CreateResponse(HttpStatusCode.Accepted);
            await res.WriteAsJsonAsync(job);
            return res;
        }
        catch (KeyNotFoundException)
        {
            return await ProblemResponse(req, HttpStatusCode.NotFound, "Not found", "Search job not found.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return await ProblemResponse(req, HttpStatusCode.Unauthorized, "Unauthorised", ex.Message);
        }
    }

    private static async Task<HttpResponseData> ProblemResponse(
        HttpRequestData req,
        HttpStatusCode code,
        string title,
        string detail)
    {
        var res = req.CreateResponse(code);
        await res.WriteAsJsonAsync(new Problem("about:blank", title, (int)code, detail, null));
        return res;
    }
}
