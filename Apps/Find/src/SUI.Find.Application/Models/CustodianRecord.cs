using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.Find.Application.Models;

public record CustodianRecord
{
    public string RecordId { get; init; } = string.Empty;
    public string PersonId { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public int Version { get; init; }
    public string SchemaUri { get; init; } = string.Empty;
    public List<ContactDetails>? ContactDetails { get; init; }

    public List<RecordLink>? RecordLinks { get; init; }

    [OpenApiProperty(
        Nullable = true,
        Description = "The payload of the record, if available. An open object that can contain any number and type of properties, as defined by the `RecordType` and `SchemaUri` properties of this object."
    )]
    public object? Payload { get; init; }
}
