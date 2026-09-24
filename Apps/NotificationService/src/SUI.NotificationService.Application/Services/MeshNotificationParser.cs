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
        "https://hl7.org/fhir/uv/subscriptions-backport/StructureDefinition/backport-subscription-status-r4";

    private const string AdditionalContextParameter = "additional-context";
    private const string SubjectPart = "subject";

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

    /// <summary>
    /// Reads the NHS number the notification is about: the <c>subject</c> part of the
    /// <c>additional-context</c> parameter. Returns false when it is missing or blank, so the caller
    /// can leave a notification it cannot act on unacknowledged.
    /// </summary>
    public static bool TryGetNhsNumber(Bundle bundle, [NotNullWhen(true)] out string? nhsNumber)
    {
        nhsNumber = null;

        if (bundle.Entry.FirstOrDefault()?.Resource is not Parameters parameters)
        {
            return false;
        }

        var subject = parameters
            .Parameter.FirstOrDefault(parameter => parameter.Name == AdditionalContextParameter)
            ?.Part.FirstOrDefault(part => part.Name == SubjectPart);

        var value = (subject?.Value as ResourceReference)?.Identifier?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        nhsNumber = value;
        return true;
    }

    private static bool IsRecordChangeNotification(Bundle bundle) =>
        bundle.Type == Bundle.BundleType.History
        && bundle.Entry.FirstOrDefault()?.Resource is Parameters parameters
        && parameters.Meta?.Profile.Contains(SubscriptionStatusProfile) == true;
}
