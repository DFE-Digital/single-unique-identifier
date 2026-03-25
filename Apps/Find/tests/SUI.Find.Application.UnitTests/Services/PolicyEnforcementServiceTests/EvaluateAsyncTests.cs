using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.UnitTests.Services.PolicyEnforcementServiceTests;

public class EvaluateAsyncTests
{
    private readonly PolicyEnforcementService _sut;
    private readonly FakeTimeProvider _fakeClock = new();

    public EvaluateAsyncTests()
    {
        var logger = Substitute.For<ILogger<PolicyEnforcementService>>();
        _fakeClock.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        _sut = new PolicyEnforcementService(logger, _fakeClock);
    }

    [Fact]
    public async Task LocalAuthority_CanAccessHealthRecords_ForSafeguarding()
    {
        // Arrange - HEALTH-01's policy allows LOCAL_AUTHORITY to see health records
        var policy = new DsaPolicyDefinition
        {
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["health.details"],
                    DestOrgTypes = ["LOCAL_AUTHORITY", "HEALTH", "POLICE"],
                    Purposes = ["SAFEGUARDING", "CHILD_PROTECTION"],
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            ],
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "HEALTH-01",
            DestinationOrgId: "LOCAL-AUTHORITY-01",
            RecordType: "health.details",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        // Act
        var result = await _sut.EvaluateAsync(request, policy, "LOCAL_AUTHORITY");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Contains("allow", result.Reason);
        Assert.Contains("default", result.Reason);
    }

    [Fact]
    public async Task Health_CanAccessLocalAuthorityRecords_ForSafeguarding()
    {
        // Arrange - LOCAL-AUTHORITY-01's policy allows HEALTH to see CSC records
        var policy = new DsaPolicyDefinition
        {
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["health.details"],
                    DestOrgTypes = ["LOCAL_AUTHORITY", "EDUCATION", "HEALTH", "POLICE"],
                    Purposes = ["SAFEGUARDING", "CHILD_PROTECTION"],
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            ],
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "LOCAL-AUTHORITY-01",
            DestinationOrgId: "HEALTH-01",
            RecordType: "health.details",
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
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["crime-justice.details"],
                    DestOrgTypes = ["POLICE"], // Only POLICE orgs
                    Purposes = ["SAFEGUARDING", "CRIME_PREVENTION"],
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            ],
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "POLICE-01",
            DestinationOrgId: "EDUCATION-01",
            RecordType: "crime-justice.details",
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
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["crime-justice.details"],
                    DestOrgTypes = ["POLICE"], // Only POLICE by default
                    Purposes = ["SAFEGUARDING"],
                },
            ],
            Exceptions =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE", "CONTENT"],
                    RecordTypes = ["crime-justice.details"],
                    DestOrgIds = ["LOCAL-AUTHORITY-01"], // Specific org exception
                    DestOrgTypes = [], // Empty is fine when using destOrgIds
                    Purposes = ["SAFEGUARDING", "CHILD_PROTECTION"],
                    ValidFrom = DateTimeOffset.Parse("2025-12-01T00:00:00Z"),
                    ValidUntil = DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
                    Reason = "Timeboxed multi-agency safeguarding operation.",
                },
            ],
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "POLICE-01",
            DestinationOrgId: "LOCAL-AUTHORITY-01",
            RecordType: "crime-justice.details",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        // Act
        var result = await _sut.EvaluateAsync(request, policy, "LOCAL_AUTHORITY");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Contains("allow", result.Reason);
        Assert.Contains("exception", result.Reason);
    }

    [Fact]
    public async Task Denies_WhenNoDestinationTargetingSpecified()
    {
        var policy = new DsaPolicyDefinition
        {
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["health.details"],
                    DestOrgIds = [], // explicitly none
                    DestOrgTypes = [], // explicitly none
                    Purposes = ["SAFEGUARDING"],
                    ValidFrom = DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
                },
            ],
        };

        var request = new PolicyDecisionRequest(
            SourceOrgId: "HEALTH-01",
            DestinationOrgId: "LOCAL-AUTHORITY-01",
            RecordType: "health.details",
            Mode: ShareMode.Existence,
            Purpose: "SAFEGUARDING"
        );

        var result = await _sut.EvaluateAsync(request, policy, "LOCAL_AUTHORITY");
        Assert.False(result.IsAllowed);
        Assert.Contains("No matching rule", result.Reason);
    }

    [Fact]
    public async Task FilterResultsAsync_DoesEvaluateAsExpected()
    {
        // Arrange - HEALTH-01's policy allows LOCAL_AUTHORITY org types to see health records
        var policy = new DsaPolicyDefinition
        {
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["health.details"],
                    DestOrgTypes = ["LOCAL_AUTHORITY", "HEALTH", "POLICE"],
                    Purposes = ["SAFEGUARDING", "CHILD_PROTECTION"],
                    ValidFrom = DateTimeOffset.Parse(input: "2025-01-01T00:00:00Z"),
                },
            ],
        };

        const string sourceOrgId = "HEALTH-01";

        CustodianSearchResultItem[] searchResultItems =
        [
            new(sourceOrgId, "health.details", "record-1", "", "", null),
            new(sourceOrgId, "other.details", "record-2", "", "", null),
        ];

        const string destOrgId = "LOCAL-AUTHORITY-01";

        // Act
        var result = await _sut.FilterResultsAsync(
            sourceOrgId: sourceOrgId,
            destOrgId: destOrgId,
            destOrgType: "LOCAL_AUTHORITY",
            searchResultItems,
            policy,
            purpose: "SAFEGUARDING",
            CancellationToken.None
        );

        // Assert
        result
            .Should()
            .BeEquivalentTo([
                new
                {
                    SourceOrgId = sourceOrgId,
                    DestOrgId = destOrgId,
                    Decision = new { IsAllowed = true },
                    Item = new
                    {
                        CustodianId = sourceOrgId,
                        RecordType = "health.details",
                        RecordUrl = "record-1",
                    },
                },
                new
                {
                    SourceOrgId = sourceOrgId,
                    DestOrgId = destOrgId,
                    Decision = new { IsAllowed = false },
                    Item = new
                    {
                        CustodianId = sourceOrgId,
                        RecordType = "other.details",
                        RecordUrl = "record-2",
                    },
                },
            ]);
    }

    [Fact]
    public async Task FilterItemsAsync_DoesEvaluateAsExpected()
    {
        // Arrange - HEALTH-01's policy allows LOCAL_AUTHORITY org types to see health records
        var policy = new DsaPolicyDefinition
        {
            Defaults =
            [
                new DsaRuleDefinition
                {
                    Effect = "allow",
                    Modes = ["EXISTENCE"],
                    RecordTypes = ["health.details"],
                    DestOrgTypes = ["LOCAL_AUTHORITY", "HEALTH", "POLICE"],
                    Purposes = ["SAFEGUARDING", "CHILD_PROTECTION"],
                    ValidFrom = DateTimeOffset.Parse(input: "2025-01-01T00:00:00Z"),
                },
            ],
        };

        const string sourceOrgId = "HEALTH-01";

        ProviderDefinition[] providerDefinitions =
        [
            new()
            {
                OrgId = sourceOrgId,
                RecordType = "health.details",
                ProviderSystem = "system-a",
            },
            new()
            {
                OrgId = sourceOrgId,
                RecordType = "other.details",
                ProviderSystem = "system-b",
            },
        ];

        const string destOrgId = "LOCAL-AUTHORITY-01";

        // Act
        var result = await _sut.FilterItemsAsync(
            sourceOrgId: sourceOrgId,
            destOrgId: destOrgId,
            destOrgType: "LOCAL_AUTHORITY",
            providerDefinitions,
            policy,
            purpose: "SAFEGUARDING",
            CancellationToken.None
        );

        // Assert
        result
            .Should()
            .BeEquivalentTo([
                new
                {
                    SourceOrgId = sourceOrgId,
                    DestOrgId = destOrgId,
                    Decision = new { IsAllowed = true },
                    Item = new
                    {
                        OrgId = sourceOrgId,
                        RecordType = "health.details",
                        ProviderSystem = "system-a",
                    },
                },
                new
                {
                    SourceOrgId = sourceOrgId,
                    DestOrgId = destOrgId,
                    Decision = new { IsAllowed = false },
                    Item = new
                    {
                        OrgId = sourceOrgId,
                        RecordType = "other.details",
                        ProviderSystem = "system-b",
                    },
                },
            ]);
    }
}
