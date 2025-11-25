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

public sealed class GetSearchResultsFunction(IFindOrchestrationService orchestration)
{
    private readonly IFindOrchestrationService _orchestration = orchestration;

    [Function("GetSearchResults")]
    [RequiredScopes("find-record.read")]
    [OpenApiOperation(operationId: "getSearchResults", tags: new[] { "Find" }, Summary = "Get search results")]
    [OpenApiParameter("jobId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(SearchResults))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/searches/{jobId}/results")] HttpRequestData req,
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
            var results = await _orchestration.GetResultsAsync(durableClient, auth, jobId, context.CancellationToken);

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(results);
            return res;
        }
        catch (InvalidOperationException ex)
        {
            return await ProblemResponse(req, HttpStatusCode.Accepted, "Search running", ex.Message);
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
