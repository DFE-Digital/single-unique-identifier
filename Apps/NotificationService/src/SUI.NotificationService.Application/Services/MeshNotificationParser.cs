using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using SUI.Shared;

namespace SUI.NotificationService.Application.Services;

/// <summary>
/// Turns the raw MESH message body into a typed FHIR <see cref="Bundle"/>.
/// Parsing lives in the application layer rather than the MESH boundary because what to do with an
/// unusable payload is an acknowledgement decision, not a transport one.
/// </summary>
public static class MeshNotificationParser
{
    private const string AdditionalContextParameter = "additional-context";
    private const string SubjectPart = "subject";
    private const string EventTypePart = "event-type";
    private const string RecordChangeEventType = "pds-record-change-2";

    private static readonly JsonSerializerOptions FhirJsonOptions =
        new JsonSerializerOptions().ForFhir();

    /// <summary>
    /// Parses a MESH message body and confirms it is the shape a pds-record-change-2 notification
    /// takes: a <c>history</c> Bundle whose first entry is a <see cref="Parameters"/> resource with
    /// <c>additional-context.event-type</c> of <c>pds-record-change-2</c>. Other MNS events share the
    /// Bundle shape, so the event type is what stops a mis-subscribed event being treated as one.
    /// Returns false for anything else so the caller can leave the message unacknowledged.
    /// </summary>
    public static bool TryParse(string? content, [NotNullWhen(true)] out Bundle? bundle)
    {
        bundle = null;

        if (
            !TryDeserialize(content, out var parsed)
            || GetEventType(parsed) != RecordChangeEventType
        )
        {
            return false;
        }

        bundle = parsed;
        return true;
    }

    /// <summary>
    /// Confirms a message <see cref="TryParse"/> turned down is a well-formed MNS notification for an
    /// event we do not subscribe to. Those are ignored, whereas anything else that fails to parse may
    /// be a damaged pds-record-change-2 notification, which is a change we would otherwise lose.
    /// </summary>
    public static bool IsOtherEvent(string? content) =>
        TryDeserialize(content, out var bundle)
        && GetEventType(bundle) is { } eventType
        && eventType != RecordChangeEventType;

    /// <summary>
    /// Reads the NHS number the notification is about: the <c>subject</c> part of the
    /// <c>additional-context</c> parameter. Returns false when it is missing or is not a valid NHS
    /// number, so the caller can leave a notification it cannot act on unacknowledged rather than
    /// pass malformed source data on to suppliers.
    /// </summary>
    public static bool TryGetNhsNumber(Bundle bundle, [NotNullWhen(true)] out string? nhsNumber)
    {
        nhsNumber = null;

        var subject = GetAdditionalContextPart(bundle, SubjectPart);

        var value = (subject?.Value as ResourceReference)?.Identifier?.Value;

        if (!NhsNumberValidator.IsValid(value))
        {
            return false;
        }

        nhsNumber = value;
        return true;
    }

    private static bool TryDeserialize(string? content, [NotNullWhen(true)] out Bundle? bundle)
    {
        bundle = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            bundle = JsonSerializer.Deserialize<Bundle>(content, FhirJsonOptions);
        }
        catch (Exception exception)
            when (exception is DeserializationFailedException or JsonException)
        {
            return false;
        }

        return bundle is not null;
    }

    private static string? GetEventType(Bundle bundle) =>
        bundle.Type == Bundle.BundleType.History
            ? (GetAdditionalContextPart(bundle, EventTypePart)?.Value as FhirString)?.Value
            : null;

    private static Parameters.ParameterComponent? GetAdditionalContextPart(
        Bundle bundle,
        string partName
    ) =>
        (bundle.Entry.FirstOrDefault()?.Resource as Parameters)
            ?.Parameter.FirstOrDefault(parameter => parameter.Name == AdditionalContextParameter)
            ?.Part.FirstOrDefault(part => part.Name == partName);
}
