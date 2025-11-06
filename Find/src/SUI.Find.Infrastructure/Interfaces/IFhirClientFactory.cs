using Hl7.Fhir.Rest;

namespace SUI.Find.Infrastructure.Interfaces;

public interface IFhirClientFactory
{
    FhirClient CreateFhirClient();
}