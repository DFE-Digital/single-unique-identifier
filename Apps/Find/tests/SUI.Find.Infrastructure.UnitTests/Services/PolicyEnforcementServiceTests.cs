using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models.Policy;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class PolicyEnforcementServiceTests
{
    private readonly ICustodianService _mockCustodianService = Substitute.For<ICustodianService>();
    private readonly ILogger<PolicyEnforcementService> _mockLogger = Substitute.For<ILogger<PolicyEnforcementService>>();
    private readonly IPolicyCompilerService _mockPolicyCompiler = Substitute.For<IPolicyCompilerService>();
    private readonly IPolicyEnforcementService _mockPep;

    public PolicyEnforcementServiceTests()
    {
        _mockPep = new PolicyEnforcementService(_mockLogger, _mockCustodianService, _mockPolicyCompiler);
    }


    [Fact]
    public async Task EvaluateDsaAsync_ShouldReturnAllowed_WhenPolicyExists()
    {
        // Arrange
        var expectedKey = PolicyKeyFactory.CreateKey("ORG-A", "ORG-B", "EXISTENCE", "PTR", "SAFEGUARDING");
        var artefact = new CompiledPolicyArtefact
        {
            PolicyVersionId = "v1-allow-test",
            CompiledAtUtc = DateTime.UtcNow,
            AllowedRequests = new[] { expectedKey }.ToFrozenSet()
        };

        _mockCustodianService.GetCustodiansAsync().Returns(new List<ProviderDefinition>());
        _mockPolicyCompiler.Compile(Arg.Any<IEnumerable<ProviderDefinition>>()).Returns(artefact);

        var request = new PolicyCheckRequest("ORG-A", "ORG-B", "PTR", "SAFEGUARDING", "EXISTENCE");

        // Act
        var result = await _mockPep.EvaluateDsaAsync(request);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal("Allowed", result.Reason);
        Assert.Equal("v1-allow-test", result.PolicyVersionId);
    }

    [Fact]
    public async Task EvaluateDsaAsync_ShouldReturnDenied_WhenPolicyDoesNotMatch()
    {
        // Arrange
        var allowedKey = PolicyKeyFactory.CreateKey("ORG-A", "ORG-C", "EXISTENCE", "PTR", "SAFEGUARDING");

        var artefact = new CompiledPolicyArtefact
        {
            PolicyVersionId = "v1-deny-test",
            CompiledAtUtc = DateTime.UtcNow,
            AllowedRequests = new[] { allowedKey }.ToFrozenSet()
        };

        _mockCustodianService.GetCustodiansAsync().Returns(new List<ProviderDefinition>());
        _mockPolicyCompiler.Compile(Arg.Any<IEnumerable<ProviderDefinition>>()).Returns(artefact);
        var request = new PolicyCheckRequest("ORG-A", "ORG-B", "PTR", "SAFEGUARDING", "EXISTENCE");

        // Act
        var result = await _mockPep.EvaluateDsaAsync(request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal("Denied", result.Reason);
        Assert.False(string.IsNullOrEmpty(result.PolicyVersionId));
    }
}