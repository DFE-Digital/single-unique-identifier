using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using SUI.NotificationService.Application.Services;
using SUI.NotificationService.Application.UnitTests.Fixtures;
using static SUI.NotificationService.Application.UnitTests.Fixtures.MeshNotificationFixtures;

namespace SUI.NotificationService.Application.UnitTests.Services;

public sealed class MeshNotificationParserTests
{
    [Fact]
    public void TryParse_ReturnsBundle_WhenBodyIsRecordChangeNotification()
    {
        var parsed = MeshNotificationParser.TryParse(BuildNotification(), out var bundle);

        Assert.True(parsed);
        Assert.NotNull(bundle);
        Assert.Equal(MeshNotificationFixtures.BundleId, bundle.Id);
        Assert.Equal(Bundle.BundleType.History, bundle.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{\"resourceType\":\"Bundle\"")]
    public void TryParse_ReturnsFalse_WhenBodyIsNotParseable(string content)
    {
        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBodyIsNotAFhirResource()
    {
        var parsed = MeshNotificationParser.TryParse("{\"some\":\"object\"}", out var bundle);

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
    public void TryParse_ReturnsBundle_RegardlessOfDeclaredProfile()
    {
        var content = BuildNotification(amend: notification =>
            ParametersResource(notification)["meta"]!["profile"]![0] =
                "https://hl7.org/fhir/uv/subscriptions-backport/StructureDefinition/backport-subscription-status-r4"
        );

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.True(parsed);
        Assert.NotNull(bundle);
    }

    [Theory]
    [InlineData("pds-record-change-1")]
    [InlineData("PDS-RECORD-CHANGE-2")]
    [InlineData("some-other-event")]
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
    [InlineData("pds-record-change-1")]
    public void IsOtherEvent_ShouldReturnTrue_WhenEventTypeIsNotPdsRecordChange2(string eventType)
    {
        var content = BuildNotificationWithEventType(eventType);

        Assert.True(MeshNotificationParser.IsOtherEvent(content));
    }

    [Fact]
    public void IsOtherEvent_ShouldReturnFalse_WhenEventIsPdsRecordChange2()
    {
        Assert.False(MeshNotificationParser.IsOtherEvent(BuildNotification()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json at all")]
    [InlineData("{\"some\":\"object\"}")]
    public void IsOtherEvent_ShouldReturnFalse_WhenBodyIsNotAFhirBundle(string? content)
    {
        Assert.False(MeshNotificationParser.IsOtherEvent(content));
    }

    [Fact]
    public void IsOtherEvent_ShouldReturnFalse_WhenEventTypePartIsMissing()
    {
        var content = BuildNotification(amend: notification =>
            RemoveAdditionalContextPart(notification, "event-type")
        );

        Assert.False(MeshNotificationParser.IsOtherEvent(content));
    }

    [Fact]
    public void IsOtherEvent_ShouldReturnFalse_WhenBundleIsNotAHistory()
    {
        var content = BuildNotification(amend: notification =>
        {
            notification["type"] = "searchset";
            AdditionalContextPart(notification, "event-type")["valueString"] =
                "nhs-number-change-2";
        });

        Assert.False(MeshNotificationParser.IsOtherEvent(content));
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsNhsNumber_WhenNotificationCarriesASubject()
    {
        var bundle = Parse(BuildNotification());

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.True(found);
        Assert.Equal(MeshNotificationFixtures.NhsNumber, nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectPartIsMissing()
    {
        var bundle = Parse(BuildNotification(nhsNumber: null));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectIdentifierIsBlank()
    {
        // An empty identifier is not valid FHIR and never gets this far, so blank means whitespace.
        var bundle = Parse(BuildNotification("   "));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenSubjectIdentifierIsNotAValidNhsNumber()
    {
        var bundle = Parse(BuildNotification("9000000008"));

        var found = MeshNotificationParser.TryGetNhsNumber(bundle, out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    [Fact]
    public void TryGetNhsNumber_ReturnsFalse_WhenBundleHasNoParametersEntry()
    {
        var found = MeshNotificationParser.TryGetNhsNumber(new Bundle(), out var nhsNumber);

        Assert.False(found);
        Assert.Null(nhsNumber);
    }

    private static Bundle Parse(string content)
    {
        Assert.True(MeshNotificationParser.TryParse(content, out var bundle));
        return bundle!;
    }
}
