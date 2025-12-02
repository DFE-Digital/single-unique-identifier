using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.FindApi.Functions.ProviderTriggers;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class GetProvidersFunctionTests
{
    private readonly ILogger<GetProvidersFunction> _mockLogger;
    private readonly FunctionContext _mockContext;
    private readonly GetProvidersFunction _function;

    public GetProvidersFunctionTests()
    {
        _mockLogger = Substitute.For<ILogger<GetProvidersFunction>>();
        _mockContext = Substitute.For<FunctionContext>();
        _function = new GetProvidersFunction(_mockLogger);
    }

    [Fact]
    public async Task GetProviders_ReturnsExpectedProviderList()
    {
        // Arrange
        string suid = "1234567890123456";

        // Act
        var result = await _function.GetProviders(suid, _mockContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); ;
        Assert.Equal("12345", result[0].OrgId);
        Assert.Equal("Test Provider 1", result[0].OrgName);
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains($"Get Providers triggered for SUID: {suid}")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
        
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains($"Get Providers request completed for SUID: {suid}")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}