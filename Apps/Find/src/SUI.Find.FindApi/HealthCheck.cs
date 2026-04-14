using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi;

[ExcludeFromCodeCoverage(Justification = "Simple health check endpoint")]
public class HealthCheck(
    ILogger<HealthCheck> logger,
    IHostEnvironment env,
    HealthCheckService healthCheckService
)
{
    private const string ServiceName = nameof(FindApi);

    [OpenApiOperation(
        operationId: "health-check",
        tags: ["Health"],
        Summary = "Check service is up"
    )]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
    [Function(nameof(HealthCheck))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req
    )
    {
        logger.LogInformation("Health check triggered.");

        var healthStatus = await healthCheckService.CheckHealthAsync();

        return await HttpResponseUtility.OkResponse(
            req,
            new
            {
                Value = Enum.GetName(healthStatus.Status),
                ServiceName,
                env.EnvironmentName,
                NowUtc = DateTimeOffset.UtcNow,
                NowLocal = DateTimeOffset.Now,
                BuildNumberUtility.BuildNumber,
            }
        );
    }
}
