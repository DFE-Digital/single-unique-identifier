using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.AuthEmulator.Controllers;

namespace SUI.AuthEmulator.UnitTests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void Get_Test()
    {
        // Arrange
        var sut = new HealthController(
            Substitute.For<IHostEnvironment>(),
            Substitute.For<ILogger<HealthController>>()
        )
        {
            ControllerContext = { HttpContext = Substitute.For<HttpContext>() },
        };

        // Act
        var response = sut.Get();

        // Assert
        var valueProperty = response.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);

        var value = valueProperty.GetValue(response);
        Assert.Equal("Healthy", value);
    }
}
