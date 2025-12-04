using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.ProviderTriggers;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class GetProvidersFunctionTests
{
    private readonly ILogger<GetProvidersFunction> _mockLogger = Substitute.For<ILogger<GetProvidersFunction>>();
    private readonly ICustodianService _mockCustodianService = Substitute.For<ICustodianService>();
    private readonly GetProvidersFunction _function;

    public GetProvidersFunctionTests()
    {
        _function = new GetProvidersFunction(_mockLogger, _mockCustodianService);
    }

    [Fact]
    public async Task GetProviders_ReturnsExpectedProviderList()
    {
        // Arrange
        string suid = "1234567890123456";
        string invocationId = "invocation-id";

        var expectedProviders = new List<ProviderDefinition>
        {
            new ProviderDefinition { OrgId = "12345", OrgName = "Test Provider 1", OrgType = "Type A", ProviderSystem = "System A", ProviderName = "Provider Name 1" },
            new ProviderDefinition { OrgId = "67890", OrgName = "Test Provider 2", OrgType = "Type B", ProviderSystem = "System B", ProviderName = "Provider Name 2" },
            new ProviderDefinition { OrgId = "54321", OrgName = "Test Provider 3", OrgType = "Type C", ProviderSystem = "System C", ProviderName = "Provider Name 3" }
        };

        _mockCustodianService.GetCustodiansAsync().Returns(expectedProviders);

        // Act
        var result = await _function.GetProviders(invocationId, suid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); ;
        Assert.Equal("12345", result[0].OrgId);
        Assert.Equal("Test Provider 1", result[0].OrgName);
        await _mockCustodianService.Received(1).GetCustodiansAsync();
    }

    [Fact]
    public async Task GetProviders_ReturnsEmptyList_WhenNoProvidersAvailable()
    {
        // Arrange
        string suid = "1234567890123456";
        string invocationId = "invocation-id";
        _mockCustodianService.GetCustodiansAsync().Returns(new List<ProviderDefinition>());

        // Act
        var result = await _function.GetProviders(invocationId, suid);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    [Fact]
    public async Task GetProviders_ThrowsException_WhenServiceFails()
    {
        // Arrange
        string suid = "1234567890123456";
        string invocationId = "invocation-id";
        var expectedException = new InvalidOperationException("Datasource not available");
        _mockCustodianService.GetCustodiansAsync().ThrowsAsync(expectedException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _function.GetProviders(invocationId, suid));

        Assert.Equal("Datasource not available", ex.Message);
    }
}