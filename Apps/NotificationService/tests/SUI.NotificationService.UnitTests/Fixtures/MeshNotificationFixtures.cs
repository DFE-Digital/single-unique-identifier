using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.UnitTests.Fixtures;

/// <summary>
/// Builds realistic pds-record-change-2 notification bodies, matching
/// scripts/pds-record-change-2-notification.template.json with its placeholders filled in.
/// </summary>
internal static class MeshNotificationFixtures
{
    internal const string NhsNumber = "9000000009";
    internal const string BundleId = "d8f1a2b4-0c3d-4e5f-9a6b-7c8d9e0f1a2b";

    /// <summary>
    /// Builds a notification for <paramref name="nhsNumber"/>. A null NHS number leaves the
    /// <c>subject</c> part out altogether; a blank one keeps the part but leaves its identifier empty.
    /// </summary>
    internal static string BuildNotification(
        string? nhsNumber = NhsNumber,
        string bundleId = BundleId
    )
    {
        var subject = nhsNumber is null
            ? string.Empty
            : $$"""
                { "name": "subject", "valueReference": { "identifier": { "value": "{{nhsNumber}}" } } },
                """;

        return $$"""
            {
              "resourceType": "Bundle",
              "id": "{{bundleId}}",
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
                          {{subject}}
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
}
