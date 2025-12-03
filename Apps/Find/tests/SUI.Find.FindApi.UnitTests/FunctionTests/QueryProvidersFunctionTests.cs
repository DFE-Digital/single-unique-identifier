using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.ProviderTriggers;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class QueryProvidersFunctionTests
{
    private readonly ILogger<QueryProvidersFunction> _mockLogger = Substitute.For<
        ILogger<QueryProvidersFunction>
    >();
    private readonly FunctionContext _mockContext;
    private readonly QueryProvidersFunction _function;

    public QueryProvidersFunctionTests()
    {
        _mockContext = Substitute.For<FunctionContext>();
        _function = new QueryProvidersFunction(_mockLogger);
    }

    [Fact]
    public async Task QueryProvider_ReturnsMappedSearchResult()
    {
        // Arrange
        var providerDef = new ProviderDefinition
        {
            OrgId = "org1",
            OrgName = "Provider 1",
            OrgType = "Type A",
            ProviderSystem = "System A",
            ProviderName = "Provider Name 1",
            RecordType = "Test Record Type",
        };
        var input = new QueryProviderInput(
            "test-client-1",
            "instance-123",
            "invocation-123",
            "1234567890123456",
            providerDef
        );

        // Act
        var result = await _function.QueryProvider(_mockContext, input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("System A", result[0].ProviderSystem);
        Assert.Equal("Provider Name 1", result[0].ProviderName);
        Assert.Equal("Test Record Type", result[0].RecordType);
        Assert.Equal(
            $"https://example.com/record/{providerDef.OrgId}/{input.InstanceId}",
            result[0].RecordUrl
        );
    }
}
