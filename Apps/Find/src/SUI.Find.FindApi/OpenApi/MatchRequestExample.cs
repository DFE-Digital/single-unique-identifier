using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json.Serialization;
using SUI.Find.Application.Constants.Matching;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.FindApi.OpenApi;

// Generates the JSON Example in the Swagger UI
public class MatchRequestExample : OpenApiExample<MatchRequest>
{
    public override IOpenApiExample<MatchRequest> Build(NamingStrategy namingStrategy = null)
    {
        Examples.Add(
            OpenApiExampleResolver.Resolve(
                "MatchRequestExample",
                new MatchRequest
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
                        new MatchRequestMetadata
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
