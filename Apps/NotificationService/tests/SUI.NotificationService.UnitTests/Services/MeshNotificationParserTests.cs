using Hl7.Fhir.Model;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.UnitTests.Services;

public sealed class MeshNotificationParserTests
{
    [Fact]
    public void TryParse_ReturnsBundle_WhenBodyIsRecordChangeNotification()
    {
        var parsed = MeshNotificationParser.TryParse(BuildNotification(), out var bundle);

        Assert.True(parsed);
        Assert.NotNull(bundle);
        Assert.Equal("d8f1a2b4-0c3d-4e5f-9a6b-7c8d9e0f1a2b", bundle.Id);
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
        var content = BuildNotification()
            .Replace("\"type\": \"history\"", "\"type\": \"searchset\"");

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenFirstEntryIsNotASubscriptionStatus()
    {
        var content = BuildNotification()
            .Replace(MeshNotificationParser.SubscriptionStatusProfile, "http://example.org/other");

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    [Fact]
    public void TryParse_ReturnsFalse_WhenBundleHasNoEntries()
    {
        var content = """
            {
              "resourceType": "Bundle",
              "id": "d8f1a2b4-0c3d-4e5f-9a6b-7c8d9e0f1a2b",
              "type": "history",
              "timestamp": "2026-09-22T09:15:00+00:00"
            }
            """;

        var parsed = MeshNotificationParser.TryParse(content, out var bundle);

        Assert.False(parsed);
        Assert.Null(bundle);
    }

    /// <summary>
    /// A realistic pds-record-change-2 notification, matching
    /// scripts/pds-record-change-2-notification.template.json with its placeholders filled in.
    /// </summary>
    private static string BuildNotification() =>
        $$"""
            {
              "resourceType": "Bundle",
              "id": "d8f1a2b4-0c3d-4e5f-9a6b-7c8d9e0f1a2b",
              "type": "history",
              "timestamp": "2026-09-22T09:15:00+00:00",
              "entry": [
                {
                  "fullUrl": "urn:uuid:3f2c1d9e-5b6a-4c7d-8e9f-0a1b2c3d4e5f",
                  "resource": {
                    "resourceType": "Parameters",
                    "meta": {
                      "profile": [
                        "{{MeshNotificationParser.SubscriptionStatusProfile}}"
                      ]
                    },
                    "id": "3f2c1d9e-5b6a-4c7d-8e9f-0a1b2c3d4e5f",
                    "parameter": [
                      {
                        "name": "subscription",
                        "valueReference": { "reference": "Subscription/00000000-0000-0000-0000-000000000001" }
                      },
                      { "name": "status", "valueCode": "active" },
                      { "name": "type", "valueCode": "event-notification" },
                      {
                        "name": "notification-event",
                        "part": [
                          { "name": "event-number", "valueString": "1" },
                          { "name": "timestamp", "valueInstant": "2026-09-22T09:15:00+00:00" }
                        ]
                      },
                      {
                        "name": "additional-context",
                        "part": [
                          { "name": "event-type", "valueString": "pds-record-change-2" },
                          { "name": "source", "valueUri": "https://fhir.nhs.uk/Id/nhsSpineASID/477121000324" },
                          {
                            "name": "subject",
                            "valueReference": { "identifier": { "value": "9000000009" } }
                          },
                          { "name": "version-id", "valueString": "W/\"2\"" }
                        ]
                      }
                    ]
                  },
                  "request": {
                    "method": "GET",
                    "url": "Subscription/00000000-0000-0000-0000-000000000001"
                  },
                  "response": { "status": "200" }
                }
              ]
            }
            """;
}
