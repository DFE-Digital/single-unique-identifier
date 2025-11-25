using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Microsoft.DurableTask.Client;
using Models;

namespace Functions;

public sealed class FetchRecordFunction
{
    [Function("FetchRecord")]
    [RequiredScopes("fetch-record.read")]
    [OpenApiOperation(operationId: "fetchRecord", tags: new[] { "Fetch" }, Summary = "Fetch a record from a participating system")]
    [OpenApiParameter("recordId", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(RecordEnvelopeBase))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/records/{recordId}")] HttpRequestData req,
        string recordId)
    {
        var res = req.CreateResponse(HttpStatusCode.NotFound);
        await res.WriteAsJsonAsync(new Problem("about:blank", "Not found", 404, "Record not found.", null));
        return res;
    }
}
