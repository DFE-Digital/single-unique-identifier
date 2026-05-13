using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.Find.Application.Models.Matching;

public class MatchRequest
{
    [OpenApiProperty(
        Description = "Metadata about the record(s) of data held for the specified person, used to help the SUI System maintain data accuracy."
    )]
    public MatchRequestMetadata[]? Metadata { get; init; }

    [OpenApiProperty(Nullable = false)]
    public required PersonSpecification PersonSpecification { get; init; }
}
