using System.Text.Json.Nodes;

namespace SUI.NotificationService.Application.UnitTests.Fixtures;

/// <summary>
/// Builds pds-record-change-2 notification bodies from the example NHS England publishes
/// (Fixtures/nhs-pds-record-change-2-example.json), which is the single source of truth for their
/// schema. Each call starts from a fresh copy, so a test states only how its payload differs.
/// </summary>
internal static class MeshNotificationFixtures
{
    internal const string NhsNumber = "9912003888";
    internal const string BundleId = "deb6ed8d-c54a-4489-8609-b15baf927541";
    internal const string ParametersId = "7c38eb82-9186-402f-a120-94c29ceddb48";
    internal const string Timestamp = "2020-06-01T13:00:00Z";

    private const string AdditionalContextParameter = "additional-context";
    private const string SubjectPart = "subject";
    private const string EventTypePart = "event-type";

    internal static readonly string PublishedExample = File.ReadAllText(
        Path.Join(AppContext.BaseDirectory, "Fixtures", "nhs-pds-record-change-2-example.json")
    );

    /// <summary>
    /// Builds a notification for <paramref name="nhsNumber"/>, applying <paramref name="amend"/> to
    /// the Bundle last. A null NHS number leaves the <c>subject</c> part out altogether.
    /// </summary>
    internal static string BuildNotification(
        string? nhsNumber = NhsNumber,
        Action<JsonObject>? amend = null
    )
    {
        var bundle = JsonNode.Parse(PublishedExample)!.AsObject();

        if (nhsNumber is null)
        {
            RemoveAdditionalContextPart(bundle, SubjectPart);
        }
        else
        {
            AdditionalContextPart(bundle, SubjectPart)["valueReference"]!["identifier"]!["value"] =
                nhsNumber;
        }

        amend?.Invoke(bundle);

        return bundle.ToJsonString();
    }

    internal static string BuildNotificationWithEventType(string eventType) =>
        BuildNotification(amend: bundle =>
            AdditionalContextPart(bundle, EventTypePart)["valueString"] = eventType
        );

    internal static JsonObject ParametersResource(JsonObject bundle) =>
        bundle["entry"]![0]!["resource"]!.AsObject();

    internal static JsonObject AdditionalContext(JsonObject bundle) =>
        Named(ParametersResource(bundle)["parameter"]!.AsArray(), AdditionalContextParameter);

    internal static JsonObject AdditionalContextPart(JsonObject bundle, string name) =>
        Named(AdditionalContext(bundle)["part"]!.AsArray(), name);

    internal static void RemoveAdditionalContextPart(JsonObject bundle, string name) =>
        AdditionalContext(bundle)["part"]!.AsArray().Remove(AdditionalContextPart(bundle, name));

    private static JsonObject Named(JsonArray items, string name) =>
        items.Single(item => (string?)item!["name"] == name)!.AsObject();
}
