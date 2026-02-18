using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SUI.Find.AuditProcessor;

[ExcludeFromCodeCoverage(Justification = "Simple health check endpoint")]
public class HealthCheck(ILogger<HealthCheck> logger, HealthCheckService healthCheckService)
{
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
