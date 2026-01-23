using System.Diagnostics.CodeAnalysis;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Options;
using SUI.Find.Infrastructure.Interfaces.Fhir;
using SUI.Find.Infrastructure.Models.Fhir;

namespace SUI.Find.Infrastructure.Factories.Fhir;

[ExcludeFromCodeCoverage(Justification = "Factory class on concrete FhirClient creation")]
public class FhirClientFactory(IOptions<AuthTokenServiceConfig> nhsAuthConfig) : IFhirClientFactory
{
    public FhirClient CreateFhirClient()
    {
        // Throwing here to fail fast if the config is missing
        var baseUri =
            nhsAuthConfig.Value.NHS_DIGITAL_FHIR_ENDPOINT ?? throw new ArgumentNullException();

        // TODO: Add auth headers in next phase of work
        return new FhirClient(new Uri(baseUri));
    }
}
