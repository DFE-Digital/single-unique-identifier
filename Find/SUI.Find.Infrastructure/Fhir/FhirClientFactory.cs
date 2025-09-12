using Hl7.Fhir.Rest;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Fhir;

public class FhirClientFactory : IFhirClientFactory
{
    public FhirClient CreateFhirClient()
    {
        // TODO: Stub.
        // Once we have config, Implementation to create and configure the FhirClient
        return new FhirClient("https://example.com/fhir");
    }
}