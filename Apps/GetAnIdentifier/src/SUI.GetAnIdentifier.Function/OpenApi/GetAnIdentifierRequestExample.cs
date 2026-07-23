using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json.Serialization;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Function.Constants;
using SUI.GetAnIdentifier.Function.Models;

namespace SUI.GetAnIdentifier.Function.OpenApi;

// Generates the JSON Example in the Swagger UI
[ExcludeFromCodeCoverage(
    Justification = "OpenAPI request example does not contain any logic to be tested."
)]
public class GetAnIdentifierRequestExample : OpenApiExample<GetAnIdentifierRequest>
{
    public override IOpenApiExample<GetAnIdentifierRequest> Build(
        NamingStrategy? namingStrategy = null
    )
    {
        Examples.Add(
            OpenApiExampleResolver.Resolve(
                "GetAnIdentifierRequestExample",
                new GetAnIdentifierRequest
                {
                    PersonSpecification = new PersonSpecification
                    {
                        Given = "Octavia",
                        Family = "Chislett",
                        BirthDate = new DateOnly(2022, 3, 17),
                        Gender = PdsConstants.Gender.Female,
                        AddressPostalCode = "KT19 0ST",
                    },
                    Metadata =
                    [
                        new GetAnIdentifierRequestMetadata
                        {
                            RecordType = "health.details",
                            SystemId = "SYS-XYZ",
                            RecordId = "987123",
                        },
                    ],
                },
                namingStrategy
            )
        );

        return this;
    }
}
