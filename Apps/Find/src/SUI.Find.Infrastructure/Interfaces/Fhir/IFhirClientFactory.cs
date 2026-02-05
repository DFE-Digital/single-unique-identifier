using Hl7.Fhir.Rest;

namespace SUI.Find.Infrastructure.Interfaces.Fhir;

public interface IFhirClientFactory
{
    Task<FhirClient> CreateFhirClientAsync(CancellationToken cancellationToken = default);
}
