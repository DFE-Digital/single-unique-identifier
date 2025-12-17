using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.Utility;


namespace SUI.Find.Infrastructure.UnitTests.Services;

public class PolicyCompilerTests
{
    private readonly PolicyCompiler _mockPolicyCompiler;

    public PolicyCompilerTests()
    {
        _mockPolicyCompiler = new PolicyCompiler();
    }

    private static ProviderDefinition CreateProvider(string orgId, string orgType)
    {
        return new ProviderDefinition
        {
            OrgId = orgId,
            OrgType = orgType,
            OrgName = "Test Org",
            DsaPolicy = new DsaPolicyDefinition
            {
                Version = DateTimeOffset.UtcNow,
                Defaults = new List<DsaRuleDefinition>()
            }
        };
    }

    private static DsaRuleDefinition CreateRule(string effect,
        string[]? destOrgIds = null,
        string[]? destOrgTypes = null,
        DateTimeOffset? validFrom = null,
        DateTimeOffset? validUntil = null)
    {
        return new DsaRuleDefinition
        {
            Effect = effect,
            Modes = ["EXISTENCE"],
            DataTypes = ["PTR"],
            Purposes = ["SAFEGUARDING"],
            DestOrgIds = destOrgIds?.ToList() ?? [],
            DestOrgTypes = destOrgTypes?.ToList() ?? [],
            ValidFrom = validFrom ?? DateTimeOffset.UtcNow.AddDays(-1),
            ValidUntil = validUntil ?? DateTimeOffset.UtcNow.AddDays(1)
        };
    }

    [Fact]
    public void PolicyCompiler_ShouldCreateKeys_WhenRuleIsExplicitlyAllowed()
    {
        // Arrange
        var provider = CreateProvider("ORG-A", "GP");
        var rule = CreateRule(effect: "allow", destOrgIds: ["ORG-B"]);

        provider.DsaPolicy.Defaults.Add(rule);

        var providers = new List<ProviderDefinition> { provider };

        // Act
        var result = _mockPolicyCompiler.Compile(providers);
        var expectedKey = PolicyKeyFactory.CreateKey("ORG-A", "ORG-B", "EXISTENCE", "PTR", "SAFEGUARDING");

        // Assert

        Assert.Contains(expectedKey, result.AllowedRequests.Items);
    }

    [Fact]
    public void PolicyCompilerShouldCreateKeys_WhenDestOrgTypeMatches()
    {
        // Arrange
        var sourceProvider = CreateProvider("ORG-A", "GP");
        var rule = CreateRule(effect: "allow", destOrgTypes: ["POLICE"]);

        sourceProvider.DsaPolicy.Defaults.Add(rule);

        var destProvider = CreateProvider("ORG-B", "POLICE");

        var providers = new List<ProviderDefinition> { sourceProvider, destProvider };

        // Act
        var result = _mockPolicyCompiler.Compile(providers);
        var expectedKey = PolicyKeyFactory.CreateKey("ORG-A", "ORG-B", "EXISTENCE", "PTR", "SAFEGUARDING");

        // Assert
        Assert.Contains(expectedKey, result.AllowedRequests.Items);
    }

    [Fact]
    public void PolicyCompiler_ShouldNotCreateKey_WhenEffectIsDeny()
    {
        // Arrange
        var provider = CreateProvider("ORG-A", "GP");
        var rule = CreateRule(effect: "deny", destOrgIds: ["ORG-B"]);

        provider.DsaPolicy.Defaults.Add(rule);

        var providers = new List<ProviderDefinition> { provider };

        // Act
        var result = _mockPolicyCompiler.Compile(providers);

        // Assert
        Assert.Empty(result.AllowedRequests);
    }

    [Fact]
    public void PolicyCompiler_ShouldNotCreateKey_WhenRuleIsExpired()
    {
        // Arrange
        var provider = CreateProvider("ORG-A", "GP");
        var rule = CreateRule(effect: "allow", destOrgIds: ["ORG-B"], validUntil: DateTimeOffset.UtcNow.AddDays(-1));
        // expired rule

        var providers = new List<ProviderDefinition> { provider };

        provider.DsaPolicy.Defaults.Add(rule);

        // Act
        var result = _mockPolicyCompiler.Compile(providers);

        // Assert
        Assert.Empty(result.AllowedRequests);
    }

    [Fact]
    public void Compile_ShouldNotCreateKeys_WhenRuleIsFuture()
    {
        // Arrange
        var provider = CreateProvider("ORG-A", "GP");
        var rule = CreateRule(effect: "allow", destOrgIds: ["ORG-B"], validFrom: DateTimeOffset.UtcNow.AddDays(1));

        provider.DsaPolicy.Defaults.Add(rule);

        var providers = new List<ProviderDefinition> { provider };

        // Act
        var result = _mockPolicyCompiler.Compile(providers);

        // Assert
        Assert.Empty(result.AllowedRequests);
    }
}