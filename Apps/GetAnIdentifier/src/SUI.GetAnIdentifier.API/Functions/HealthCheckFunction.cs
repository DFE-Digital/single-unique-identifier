using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.GetAnIdentifier.API.Utility;

namespace SUI.GetAnIdentifier.API.Functions;

public class HealthCheckFunction(
    ILogger<HealthCheckFunction> logger,
    IHostEnvironment env,
    HealthCheckService healthCheckService
)
{
    private const string ServiceName = nameof(GetAnIdentifier);

    [OpenApiOperation(
        operationId: "health-check",
        tags: ["Health"],
        Summary = "Check service is up"
    )]
    [Function(nameof(HealthCheckFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req
    )
    {
        logger.LogInformation("Health check triggered.");

        var healthStatus = await healthCheckService.CheckHealthAsync();

        var statusCode = healthStatus.Status switch
        {
            HealthStatus.Healthy or HealthStatus.Degraded => HttpStatusCode.OK,
            _ => HttpStatusCode.ServiceUnavailable,
        };

        return await HttpResponseUtility.JsonResponse(
            req,
            statusCode,
            new
            {
                Value = healthStatus.Status.ToString(),
                ServiceName,
                env.EnvironmentName,
                NowUtc = DateTimeOffset.UtcNow,
                NowLocal = DateTimeOffset.Now,
                BuildTimestampUtility.BuildTimestamp,
            }
        );
    }
}
