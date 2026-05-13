using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using SUI.Find.Application.Constants;

namespace SUI.Find.Application.Models.Matching;

public class MatchRequestMetadata
{
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
        init =>
            field = string.IsNullOrWhiteSpace(value)
                ? ApplicationConstants.SystemIds.Default
                : value;
    } = ApplicationConstants.SystemIds.Default;

    [OpenApiProperty(
        Description = "Optional. Specifies the unique identifier for the record of data."
    )]
    public string? RecordId { get; set; }
}
