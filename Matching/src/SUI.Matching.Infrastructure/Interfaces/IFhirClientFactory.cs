using Hl7.Fhir.Rest;

namespace SUI.Matching.Infrastructure.Interfaces;

public interface IFhirClientFactory
{
    FhirClient CreateFhirClient();
}
