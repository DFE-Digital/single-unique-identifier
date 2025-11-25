using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.DurableTask.Client;
using Models;
using Interfaces;

namespace Functions;

public sealed class StartSearchFunction(IFindOrchestrationService orchestration)
{
    private readonly IFindOrchestrationService _orchestration = orchestration;

    [Function("StartSearch")]
    [RequiredScopes("find-record.write")]
    [OpenApiOperation(operationId: "startSearch", tags: new[] { "Find" }, Summary = "Start a Find a Record search")]
    [OpenApiRequestBody("application/json", typeof(StartSearchRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.Accepted, "application/json", typeof(SearchJob))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/searches")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        if (!context.Items.TryGetValue("AuthContext", out var authObj) || authObj is not AuthContext auth)
        {
            var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauth.WriteAsJsonAsync(
                new Problem("about:blank", "Unauthorised", 401, "Missing or invalid bearer token.", null));
            return unauth;
        }

        var body = await req.ReadFromJsonAsync<StartSearchRequest>();

        if (body is null || string.IsNullOrWhiteSpace(body.PersonId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(
                new Problem("about:blank", "Invalid request", 400, "Missing or malformed personId.", null));
            return bad;
        }

        try
        {
            var job = await _orchestration.StartSearchAsync(
                durableClient,
                auth,
                body.PersonId,
                context.CancellationToken);

            var res = req.CreateResponse(HttpStatusCode.Accepted);
            await res.WriteAsJsonAsync(job);
            return res;
        }
        catch (UnauthorizedAccessException ex)
        {
            var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauth.WriteAsJsonAsync(
                new Problem("about:blank", "Unauthorised", 401, ex.Message, null));
            return unauth;
        }
    }
}
