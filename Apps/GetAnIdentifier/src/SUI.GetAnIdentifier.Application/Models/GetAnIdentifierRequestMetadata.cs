using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.GetAnIdentifier.Application.Models;

public class GetAnIdentifierRequestMetadata
{
    private const string DefaultSystemId = "DefaultSystem";

    [OpenApiProperty(
        Description = "The type of the record of data about a person",
        Nullable = false
    )]
    public required string RecordType { get; set; }

    [OpenApiProperty(
        Description = "Optional. Specifies the unique identifier for the system that holds the record of data."
    )]
    public string SystemId
    {
        get;
        init => field = string.IsNullOrWhiteSpace(value) ? DefaultSystemId : value;
    } = DefaultSystemId;

    [OpenApiProperty(
        Description = "Optional. Specifies the unique identifier for the record of data."
    )]
    public string? RecordId { get; set; }
}
