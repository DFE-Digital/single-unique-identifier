using Microsoft.AspNetCore.Mvc;
using SUI.StubCustodians.API.Utility;

namespace SUI.StubCustodians.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(IHostEnvironment env, ILogger<AuthController> logger) : ControllerBase
{
    private const string ServiceName = nameof(StubCustodians);

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public object Get()
    {
        logger.LogInformation($"`{nameof(HealthController)}` was called");

        Response.Headers["x-service-name"] = ServiceName;
        Response.Headers["x-environment-name"] = env.EnvironmentName;

        return new
        {
            Value = "Healthy",
            ServiceName,
            env.EnvironmentName,
            nowUtc = DateTimeOffset.UtcNow,
            nowLocal = DateTimeOffset.Now,
            BuildTimestampUtility.BuildTimestamp,
        };
    }
}
