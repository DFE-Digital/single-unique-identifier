using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using SUI.NotificationService.Application.Services;
using static SUI.NotificationService.Application.UnitTests.Fixtures.MeshNotificationFixtures;

namespace SUI.NotificationService.Application.UnitTests.Services;

public sealed class MeshNotificationParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{\"resourceType\":\"Bundle\"")]
    [InlineData("{\"some\":\"object\"}")]
    public void TryParse_ReturnsFalse_WhenBodyIsNotParseable(string content)
    {
        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBundleIsNotAHistory()
    {
        var content = BuildNotification(amend: notification => notification["type"] = "searchset");

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ShouldAcceptNotification_WhenItCarriesAnElementTheSchemaDoesNotDefine()
    {
        var content = BuildNotification(amend: notification =>
        {
            notification["newElement"] = "value";
            AdditionalContextPart(notification, "subject")["newElement"] = "value";
        });

        Assert.True(MeshNotificationParser.TryParse(content, out _));
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenAnElementHasTheWrongType()
    {
        var content = BuildNotification(amend: notification =>
            notification["entry"]![0]!["response"]!["status"] = 200
        );

        Assert.False(MeshNotificationParser.TryParse(content, out _));
    }

    [Theory]
    [InlineData("pds-record-change-1")]
    [InlineData("PDS-RECORD-CHANGE-2")]
    [InlineData("nhs-number-change-2")]
    [InlineData("")]
    public void TryParse_ReturnsFalse_WhenEventTypeIsNotPdsRecordChange2(string eventType)
    {
        var content = BuildNotificationWithEventType(eventType);

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenEventTypePartIsMissing()
    {
        var content = BuildNotification(amend: notification =>
            RemoveAdditionalContextPart(notification, "event-type")
        );

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenAdditionalContextIsMissing()
    {
        var content = BuildNotification(amend: notification =>
            AdditionalContext(notification)["name"] = "something-else"
        );

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenEventTypeIsNotAString()
    {
        var content = BuildNotification(amend: notification =>
        {
            var eventType = AdditionalContextPart(notification, "event-type");
            eventType.Remove("valueString");
            eventType["valueCode"] = "pds-record-change-2";
        });

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenFirstEntryIsNotParameters()
    {
        var content = BuildNotification(amend: notification =>
            notification["entry"]![0]!["resource"] = new JsonObject
            {
                ["resourceType"] = "Basic",
                ["id"] = ParametersId,
            }
        );

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBundleHasNoEntries()
    {
        var content = BuildNotification(amend: notification => notification.Remove("entry"));

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Theory]
    [InlineData("nhs-number-change-2")]
    [InlineData("pds-change-of-gp-2")]
    [InlineData("pds-death-notification-2")]
    public void IsKnownOtherEvent_ShouldReturnTrue_WhenEventIsOneWeDoNotSubscribeTo(
        string eventType
    )
    {
        var content = BuildNotificationWithEventType(eventType);

        Assert.True(MeshNotificationParser.IsKnownOtherEvent(content));
    }

    [Theory]
    [InlineData("pds-record-change-2")]
    [InlineData("pds-record-change-3")]
    [InlineData("PDS-RECORD-CHANGE-2")]
    public void IsKnownOtherEvent_ShouldReturnFalse_WhenEventTypeIsNotAKnownOtherEvent(
        string eventType
    )
    {
        var content = BuildNotificationWithEventType(eventType);

        Assert.False(MeshNotificationParser.IsKnownOtherEvent(content));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json at all")]
    [InlineData("{\"some\":\"object\"}")]
    public void IsKnownOtherEvent_ShouldReturnFalse_WhenBodyIsNotAFhirBundle(string? content)
    {
        Assert.False(MeshNotificationParser.IsKnownOtherEvent(content));
    }

    [Fact]
    public void IsKnownOtherEvent_ShouldReturnFalse_WhenEventTypePartIsMissing()
    {
        var content = BuildNotification(amend: notification =>
            RemoveAdditionalContextPart(notification, "event-type")
        );

        Assert.False(MeshNotificationParser.IsKnownOtherEvent(content));
    }

    [Fact]
    public void IsKnownOtherEvent_ShouldReturnFalse_WhenBundleIsNotAHistory()
    {
        var content = BuildNotification(amend: notification =>
        {
            notification["type"] = "searchset";
            AdditionalContextPart(notification, "event-type")["valueString"] =
                "nhs-number-change-2";
        });

        Assert.False(MeshNotificationParser.IsKnownOtherEvent(content));
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectPartIsMissing()
    {
        var bundle = Parse(BuildNotification(nhsNumber: null));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    [Theory]
    // An empty identifier is not valid FHIR and never gets this far, so blank means whitespace.
    [InlineData("   ")]
    [InlineData("9000000008")]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectIdentifierIsNotAValidNhsNumber(string value)
    {
        var bundle = Parse(BuildNotification(value));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    // --- NEW: Event Details Parsing Tests ---

    [Fact]
    public void TryGetEventDetails_ReturnsTrue_AndExtractsDetails()
    {
        var expectedId = Guid.NewGuid();
        var expectedTimestamp = DateTimeOffset.Parse("2026-09-22T09:15:00+00:00");

        var bundle = new Bundle { Id = expectedId.ToString(), Timestamp = expectedTimestamp };

        var found = MeshNotificationParser.TryGetEventDetails(
            bundle,
            out var eventId,
            out var occurredAt
        );

        Assert.True(found);
        Assert.Equal(expectedId, eventId);
        Assert.Equal(expectedTimestamp, occurredAt);
    }

    [Fact]
    public void TryGetEventDetails_ReturnsFalse_WhenIdIsMissing()
    {
        var bundle = new Bundle { Id = null };

        var found = MeshNotificationParser.TryGetEventDetails(bundle, out _, out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGetEventDetails_ReturnsFalse_WhenIdIsNotGuid()
    {
        var bundle = new Bundle { Id = "not-a-valid-guid" };

        var found = MeshNotificationParser.TryGetEventDetails(bundle, out _, out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGetEventDetails_DefaultsOccurredAt_WhenTimestampIsMissing()
    {
        var expectedId = Guid.NewGuid();
        var bundle = new Bundle { Id = expectedId.ToString(), Timestamp = null };

        var beforeParse = DateTimeOffset.UtcNow;
        var found = MeshNotificationParser.TryGetEventDetails(
            bundle,
            out var eventId,
            out var occurredAt
        );
        var afterParse = DateTimeOffset.UtcNow;

        Assert.True(found);
        Assert.Equal(expectedId, eventId);
        Assert.True(occurredAt >= beforeParse && occurredAt <= afterParse);
    }

    private static Bundle Parse(string content)
    {
        Assert.True(MeshNotificationParser.TryParse(content, out var bundle));
        return bundle!;
    }
}
