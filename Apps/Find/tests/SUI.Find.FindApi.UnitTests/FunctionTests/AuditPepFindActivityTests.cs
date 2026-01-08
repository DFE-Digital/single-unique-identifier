using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.FindApi.Functions.ActivityFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class AuditPepFindActivityTests
{
    [Fact]
    public async Task ShouldCallAuditService_WhenResultsArePresent()
    {
        // Arrange
        var mockAuditService = Substitute.For<IAuditService>();
        var mockTimeProvider = TimeProvider.System;
        var logger = Substitute.For<ILogger<AuditPepFindActivity>>();
        var auditActivity = new AuditPepFindActivity(logger, mockAuditService, mockTimeProvider);

        var input = new AuditPepFindInput(
            new PolicyContext("client-1", ["scope1"], "SAFEGUARDING", "LOCAL_AUTHORITY"),
            new SearchJobMetadata("person-123", DateTime.UtcNow, "invocation-123"),
            [
                new(
                    new SearchResultItem("System A", "Provider Name 1", "RecordA", "http://url1"),
                    "org1",
                    new PolicyDecisionResult { IsAllowed = true }
                ),
            ]
        );
        var expected = input.SearchResultsWithDecisions[0];

        // Act
        await auditActivity.AuditPepFindAsync(input, CancellationToken.None);

        // Assert
        await mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(p =>
                    p.Actor.ActorId == input.PolicyContext.ClientId
                    && p.EventName == "PEP_FIND"
                    && p.ServiceName == "PolicyEnforcementPoint"
                    && ReadAuditPayloadFromEvent(p).TotalRecordsFound == 1
                    && ReadAuditPayloadFromEvent(p).TotalRecordsShared == 1
                    && ReadAuditPayloadFromEvent(p).Records.Length == 1
                    && ReadAuditPayloadFromEvent(p).Records[0].SourceOrgId == expected.SourceOrgId
                    && ReadAuditPayloadFromEvent(p).Records[0].RecordUrl == expected.Item.RecordUrl
                ),
                Arg.Any<CancellationToken>()
            );
    }

    private static PepFindPayload ReadAuditPayloadFromEvent(AuditEvent auditEvent)
    {
        return JsonSerializer.Deserialize<PepFindPayload>(auditEvent.Payload.GetRawText())!;
    }
}
