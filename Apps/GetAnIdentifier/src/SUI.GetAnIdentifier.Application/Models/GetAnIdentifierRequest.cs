using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace SUI.GetAnIdentifier.Application.Models;

public class GetAnIdentifierRequest
{
    [OpenApiProperty(Nullable = false)]
    public required PersonSpecification PersonSpecification { get; init; }
}
