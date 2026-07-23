using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.GetAnIdentifier.Application.Models;

public class GetAnIdentifierRequest
{
    [OpenApiProperty(
        Description = "Metadata about the record(s) of data held for the specified person, used to help the SUI System maintain data accuracy."
    )]
    public GetAnIdentifierRequestMetadata[]? Metadata { get; init; }

    [OpenApiProperty(Nullable = false)]
    public required PersonSpecification PersonSpecification { get; init; }
}
