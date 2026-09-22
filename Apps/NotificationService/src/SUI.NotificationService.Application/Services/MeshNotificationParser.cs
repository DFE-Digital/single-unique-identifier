using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace SUI.NotificationService.Application.Services;

/// <summary>
/// Turns the raw MESH message body into a typed FHIR <see cref="Bundle"/>.
/// Parsing lives in the application layer rather than the MESH boundary because what to do with an
/// unusable payload is an acknowledgement decision, not a transport one.
/// </summary>
internal static class MeshNotificationParser
{
    /// <summary>
    /// The R4 Subscriptions Backport profile every pds-record-change-2 notification declares on the
    /// <see cref="Parameters"/> resource it wraps.
    /// </summary>
    internal const string SubscriptionStatusProfile =
        "http://hl7.org/fhir/uv/subscriptions-backport/StructureDefinition/backport-subscription-status-r4";

    private static readonly JsonSerializerOptions FhirJsonOptions =
        new JsonSerializerOptions().ForFhir();

    /// <summary>
    /// Parses a MESH message body and confirms it is the shape a pds-record-change-2 notification
    /// takes: a <c>history</c> Bundle whose first entry is a <see cref="Parameters"/> resource
    /// declaring the Subscriptions Backport <c>SubscriptionStatus</c> profile. Returns false for
    /// anything else so the caller can leave the message unacknowledged.
    /// </summary>
    public static bool TryParse(string? content, [NotNullWhen(true)] out Bundle? bundle)
    {
        bundle = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        Bundle? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Bundle>(content, FhirJsonOptions);
        }
        catch (Exception exception)
            when (exception is DeserializationFailedException or JsonException)
        {
            return false;
        }

        if (parsed is null || !IsRecordChangeNotification(parsed))
        {
            return false;
        }

        bundle = parsed;
        return true;
    }

    private static bool IsRecordChangeNotification(Bundle bundle) =>
        bundle.Type == Bundle.BundleType.History
        && bundle.Entry.FirstOrDefault()?.Resource is Parameters parameters
        && parameters.Meta?.Profile.Contains(SubscriptionStatusProfile) == true;
}
