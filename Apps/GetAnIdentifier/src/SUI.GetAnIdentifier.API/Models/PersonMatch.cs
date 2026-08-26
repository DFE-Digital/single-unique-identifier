using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.API.Models;

public record PersonMatch(
    [property: OpenApiProperty(
        Description = "The Single Unique Identifier for an individual",
        Default = "9449305552",
        Nullable = false
    )]
        string PersonId,
    [property: OpenApiProperty(
        Description = "ODS code for the general practice with which the person is registered",
        Nullable = false
    )]
        IReadOnlyCollection<string> GeneralPractitioner
)
{
    public static PersonMatch Create(GetAnIdentifierResult getAnIdentifierResult)
    {
        return new PersonMatch(
            getAnIdentifierResult.PersonId.Value,
            getAnIdentifierResult.GeneralPractitioner
        );
    }
}
