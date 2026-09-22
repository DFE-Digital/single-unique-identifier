using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.GetAnIdentifier.API.Functions;
using SUI.GetAnIdentifier.API.UnitTests.Mocks;

namespace SUI.GetAnIdentifier.API.UnitTests.Functions;

public class HealthCheckFunctionTests
{
    private readonly ILogger<HealthCheckFunction> _logger = Substitute.For<ILogger<HealthCheckFunction>>();
    private readonly HealthCheckService _healthCheckService = Substitute.For<HealthCheckService>();
    private readonly IHostEnvironment _hostEnvironment = Substitute.For<IHostEnvironment>();

    private HealthCheckFunction CreateFunction() => new(_logger, _hostEnvironment, _healthCheckService);

    [Theory]
    [InlineData(HealthStatus.Healthy, HttpStatusCode.OK)]
    [InlineData(HealthStatus.Degraded, HttpStatusCode.OK)]
    [InlineData(HealthStatus.Unhealthy, HttpStatusCode.ServiceUnavailable)]
    public async Task TestHealthCheck(HealthStatus healthStatus, HttpStatusCode httpStatusCode)
    {
        _healthCheckService
            .CheckHealthAsync()
            .Returns(
                new HealthReport(
                    new Dictionary<string, HealthReportEntry>(),
                    healthStatus,
                    TimeSpan.FromMinutes(1)
                )
            );

        var sut = CreateFunction();
        var request = MockHttpRequestData.Create();
        var result = await sut.Run(request);

        Assert.Equal(httpStatusCode, result.StatusCode);
    }
}
