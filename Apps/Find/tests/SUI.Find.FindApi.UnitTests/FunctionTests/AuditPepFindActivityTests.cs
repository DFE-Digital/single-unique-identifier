using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.FindApi.Functions.ActivityFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class AuditPepFindActivityTests
{
    [Fact]
    public async Task ShouldCallAuditService_WhenResultsArePresent()
    {
        // Arrange
        var mockAuditClient = Substitute.For<IAuditQueueClient>();
        var logger = Substitute.For<ILogger<AuditPepFindActivity>>();
        var mockPepService = new PolicyEnforcementAndAuditingService(
            Substitute.For<IPolicyEnforcementService>(),
            mockAuditClient,
            Substitute.For<TimeProvider>(),
            Substitute.For<ILogger<PolicyEnforcementAndAuditingService>>()
        );
        var auditActivity = new AuditPepFindActivity(logger, mockPepService);

        var input = new AuditPepFindInput(
            new PolicyContext("client-1", "SAFEGUARDING", "LOCAL_AUTHORITY"),
            new SearchJobMetadata("person-123", DateTime.UtcNow, "invocation-123"),
            [
                new(
                    new CustodianSearchResultItem(
                        "test-custodian",
                        "RecordA",
                        "http://url1",
                        "System A",
                        "test org",
                        "TestRecord 1"
                    ),
                    "org1",
                    "org2",
                    new PolicyDecisionResult
                    {
                        IsAllowed = true,
                        RuleEffect = "allow",
                        Reason = "Matched policy rule",
                    }
                ),
            ]
        );
        var expected = input.SearchResultsWithDecisions[0];

        // Act
        await auditActivity.AuditPepFindAsync(input, CancellationToken.None);

        // Assert
        await mockAuditClient
            .Received(1)
            .SendAuditEventAsync(
                Arg.Is<AuditEvent>(ae => ValidateAuditEventInputs(ae, input, expected)),
                Arg.Any<CancellationToken>()
            );
    }

    private static bool ValidateAuditEventInputs(
        AuditEvent auditEvent,
        AuditPepFindInput input,
        PepResultItem<CustodianSearchResultItem> expected
    )
    {
        var payload = ReadAuditPayloadFromEvent(auditEvent);
        if (payload.TotalRecordsFound != 1)
            return false;
        if (payload.TotalRecordsShared != 1)
            return false;
        if (payload.Records.Length != 1)
            return false;
        //
        var record = payload.Records[0];
        if (record.SourceOrgId != expected.SourceOrgId)
            return false;
        if (record.RecordUrl != expected.Item.RecordUrl)
            return false;
        if (record.RecordType != expected.Item.RecordType)
            return false;
        //
        // Policy decision snapshot checks
        if (!record.IsSharedAllowed)
            return false;
        if (string.IsNullOrWhiteSpace(record.RuleType))
            return false;
        if (!string.Equals(record.RuleEffect, "allow", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(record.DecisionReason))
            return false;

        return auditEvent.Actor.ActorId == input.PolicyContext.ClientId
            && auditEvent is { EventName: "PEP_FIND", ServiceName: "PolicyEnforcementPoint" };
    }

    private static PepFindPayload ReadAuditPayloadFromEvent(AuditEvent auditEvent)
    {
        return JsonSerializer.Deserialize<PepFindPayload>(auditEvent.Payload.GetRawText())!;
    }
}
