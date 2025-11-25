using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Models;

namespace Functions;

public sealed class StatusFunction
{
    [Function("Status")]
    [OpenApiOperation(operationId: "getStatus", tags: new[] { "Status" }, Summary = "Service health check")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(HealthStatus))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new HealthStatus("ok"));
        return res;
    }
}
