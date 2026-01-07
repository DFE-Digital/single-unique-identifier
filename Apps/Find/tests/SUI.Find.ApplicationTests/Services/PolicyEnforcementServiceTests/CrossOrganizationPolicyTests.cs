using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using Xunit;

namespace SUI.Find.ApplicationTests.Services.PolicyEnforcementServiceTests;

public class CrossOrganizationPolicyTests
{
    private readonly PolicyEnforcementService _sut;

    public CrossOrganizationPolicyTests()
    {
        var logger = Substitute.For<ILogger<PolicyEnforcementService>>();
        _sut = new PolicyEnforcementService(logger);
    }

    [Fact]
    public async Task LocalAuthority_CanAccessHealthRecords_ForSafeguarding()
    {
        // Arrange - HEALTH-01's policy allows LOCAL_AUTHORITY to see health records
        var policy = new DsaPolicyDefinition
        {
            Defaults = new List<DsaRuleDefinition>
            {
                new()
                {
                    Effect = "allow",
                    Modes = new List<string> { "EXISTENCE" },
                    DataTypes = new List<string> { "mental_health_ptr" },
                    DestOrgTypes = new List<string> { "LOCAL_AUTHORITY", "HEALTH", "POLICE" },
                    Purposes = new List<string> { "SAFEGUARDING", "CHILD_PROTECTION" },
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            },
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "HEALTH-01",
            DestinationOrgId: "LOCAL-AUTHORITY-01",
            RecordType: "health.mental-health",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        // Act
        var result = await _sut.EvaluateAsync(request, policy, "LOCAL_AUTHORITY");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Contains("allow", result.Reason);
    }

    [Fact]
    public async Task Health_CanAccessLocalAuthorityRecords_ForSafeguarding()
    {
        // Arrange - LOCAL-AUTHORITY-01's policy allows HEALTH to see CSC records
        var policy = new DsaPolicyDefinition
        {
            Defaults = new List<DsaRuleDefinition>
            {
                new()
                {
                    Effect = "allow",
                    Modes = new List<string> { "EXISTENCE" },
                    DataTypes = new List<string> { "children_social_care_ptr" },
                    DestOrgTypes = new List<string>
                    {
                        "LOCAL_AUTHORITY",
                        "EDUCATION",
                        "HEALTH",
                        "POLICE",
                    },
                    Purposes = new List<string> { "SAFEGUARDING", "CHILD_PROTECTION" },
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            },
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "LOCAL-AUTHORITY-01",
            DestinationOrgId: "HEALTH-01",
            RecordType: "local-authority.children-social-care",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        // Act
        var result = await _sut.EvaluateAsync(request, policy, "HEALTH");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Contains("allow", result.Reason);
    }

    [Fact]
    public async Task Education_CannotAccessPoliceRecords_WithoutPermission()
    {
        // Arrange - POLICE-01's policy does NOT allow EDUCATION to see police records
        var policy = new DsaPolicyDefinition
        {
            Defaults = new List<DsaRuleDefinition>
            {
                new()
                {
                    Effect = "allow",
                    Modes = new List<string> { "EXISTENCE" },
                    DataTypes = new List<string> { "police_ptr" },
                    DestOrgTypes = new List<string> { "POLICE" }, // Only POLICE orgs
                    Purposes = new List<string> { "SAFEGUARDING", "CRIME_PREVENTION" },
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            },
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "POLICE-01",
            DestinationOrgId: "EDUCATION-01",
            RecordType: "crime-justice",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        // Act
        var result = await _sut.EvaluateAsync(request, policy, "EDUCATION");

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Contains("No matching rule", result.Reason);
    }

    [Fact]
    public async Task Exception_OverridesDefault_ForSpecificOrgPair()
    {
        // Arrange - Police has exception allowing LOCAL-AUTHORITY-01 access
        var policy = new DsaPolicyDefinition
        {
            Defaults = new List<DsaRuleDefinition>
            {
                new()
                {
                    Effect = "allow",
                    Modes = new List<string> { "EXISTENCE" },
                    DataTypes = new List<string> { "crime_justice_ptr" },
                    DestOrgTypes = new List<string> { "POLICE" }, // Only POLICE by default
                    Purposes = new List<string> { "SAFEGUARDING" },
                },
            },
            Exceptions = new List<DsaRuleDefinition>
            {
                new()
                {
                    Effect = "allow",
                    Modes = new List<string> { "EXISTENCE", "CONTENT" },
                    DataTypes = new List<string> { "crime_justice_ptr", "crime_justice_record" },
                    DestOrgIds = new List<string> { "LOCAL-AUTHORITY-01" }, // Specific org exception
                    DestOrgTypes = new List<string>(), // Empty is fine when using destOrgIds
                    Purposes = new List<string> { "SAFEGUARDING", "CHILD_PROTECTION" },
                    ValidFrom = DateTimeOffset.Parse("2025-12-01T00:00:00Z"),
                    ValidUntil = DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
                    Reason = "Timeboxed multi-agency safeguarding operation.",
                },
            },
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "POLICE-01",
            DestinationOrgId: "LOCAL-AUTHORITY-01",
            RecordType: "crime-justice",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        // Act
        var result = await _sut.EvaluateAsync(request, policy, "LOCAL_AUTHORITY");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Contains("exception", result.Reason);
    }
}
