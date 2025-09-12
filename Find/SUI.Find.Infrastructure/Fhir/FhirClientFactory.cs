using Hl7.Fhir.Rest;

namespace SUI.Find.Infrastructure.Fhir;

public interface IFhirClientFactory
{
    FhirClient CreateFhirClient();
}

public class FhirClientFactory : IFhirClientFactory
{
    public FhirClient CreateFhirClient()
    {
        // TODO: Stub.
        // Once we have config, Implementation to create and configure the FhirClient
        return new FhirClient("https://example.com/fhir");
    }
}