using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Interfaces;
using Models;

namespace Functions;

public sealed class FetchRecordFunction(IFindOrchestrationService orchestration)
{
    //private readonly IFetchUrlMappingStore _mappingStore;
    //private readonly IHttpClientFactory _httpClientFactory;

    private readonly IFindOrchestrationService _orchestration = orchestration;

    [Function("FetchRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(
        operationId: "fetchRecord",
        tags: new[] { "Fetch" },
        Summary = "Fetch a record from a participating system")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(RecordEnvelopeBase))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.BadGateway, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/records/{recordId}")]
        HttpRequestData req,
        string recordId,
        FunctionContext context)
    {

        if (!context.Items.TryGetValue("AuthContext", out var authObj) || authObj is not AuthContext auth)
        {
            var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauth.WriteAsJsonAsync(
                new Problem("about:blank", "Unauthorised", 401, "Missing or invalid bearer token.", null));
            return unauth;
        }

        if (string.IsNullOrWhiteSpace(recordId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(
                new Problem(
                    "about:blank",
                    "Invalid record reference",
                    (int)HttpStatusCode.BadRequest,
                    "recordId must be supplied.",
                    null),
                context.CancellationToken);
            return bad;
        }

        try
        {
            var job = await _orchestration.FetchRecordAsync(
                auth,
                recordId, context.CancellationToken);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(job, context.CancellationToken);
            return ok;
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


