using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SUI.Find.FindApi;

[ExcludeFromCodeCoverage(Justification = "Simple health check endpoint")]
public class HealthCheck(ILogger<HealthCheck> logger, HealthCheckService healthCheckService)
{
    [OpenApiOperation(
        operationId: "health-check",
        tags: ["Health"],
        Summary = "Check service is up"
    )]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
    [Function(nameof(HealthCheck))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req
    )
    {
        logger.LogInformation("Health check triggered.");

        var healthStatus = await healthCheckService.CheckHealthAsync();
        return new OkObjectResult(Enum.GetName(healthStatus.Status));
    }
}
