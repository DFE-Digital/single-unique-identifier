using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class PolicyEnforcementPointTests
{
    private readonly ICustodianService _mockCustodianService = Substitute.For<ICustodianService>();
    private readonly ILogger<PolicyEnforcementPoint> _mockLogger = Substitute.For<ILogger<PolicyEnforcementPoint>>();
    private readonly PolicyEnforcementPoint _mockPep;

    public PolicyEnforcementPointTests()
    {
        _mockPep = new PolicyEnforcementPoint(_mockLogger, _mockCustodianService);
    }


    [Fact]
    public async Task EvaluateDsaAsync_ShouldReturnAllowed_WhenPolicyExists()
    {
        // Arrange

        var provider = new ProviderDefinition
        {
            OrgId = "ORG-A",
            OrgType = "GP",
            DsaPolicy = new DsaPolicyDefinition
            {
                Defaults = new List<DsaRuleDefinition>
                {
                    new()
                    {
                        Effect = "allow",
                        DestOrgIds = ["ORG-B"],
                        Modes = ["EXISTENCE"],
                        DataTypes = ["PTR"],
                        Purposes = ["SAFEGUARDING"]
                    }
                }
            }
        };

        _mockCustodianService.GetCustodiansAsync().Returns(new List<ProviderDefinition> { provider });

        var request = new PolicyCheckRequest("ORG-A", "ORG-B", "PTR", "SAFEGUARDING", "EXISTENCE");

        // Act
        var result = await _mockPep.EvaluateDsaAsync(request);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal("Allowed", result.Reason);
        Assert.False(string.IsNullOrEmpty(result.PolicyVersionId));
    }

    [Fact]
    public async Task EvaluateDsaAsync_ShouldReturnDenied_WhenPolicyDoesNotMatch()
    {
        // Arrange
        var provider = new ProviderDefinition
        {
            OrgId = "ORG-A",
            OrgType = "GP",
            DsaPolicy = new DsaPolicyDefinition
            {
                Defaults = new List<DsaRuleDefinition>
                {
                    new() {
                        Effect = "allow",
                        DestOrgIds = ["ORG-C"],
                        Modes = ["EXISTENCE"],
                        DataTypes = ["PTR"],
                        Purposes = ["SAFEGUARDING"]
                    }
                }
            }
        };

        _mockCustodianService.GetCustodiansAsync().Returns(new List<ProviderDefinition> { provider });

        var request = new PolicyCheckRequest("ORG-A", "ORG-B", "PTR", "SAFEGUARDING", "EXISTENCE");

        // Act
        var result = await _mockPep.EvaluateDsaAsync(request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal("Denied", result.Reason);
        Assert.False(string.IsNullOrEmpty(result.PolicyVersionId));
    }
}