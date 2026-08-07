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
    IEnumerable<string?> GeneralPractitionerInformation
)
{
    public static PersonMatch Create((NhsPersonId, IEnumerable<string?>) getAnIdentifierResult)
    {
        return new PersonMatch(getAnIdentifierResult.Item1.Value, getAnIdentifierResult.Item2);
    }
}
