using Hl7.Fhir.Rest;

namespace SUI.GetAnIdentifier.Infrastructure.Interfaces;

public interface IFhirClientFactory
{
    Task<FhirClient> CreateFhirClientAsync(
        string odsCode,
        string? correlationId = null,
        CancellationToken cancellationToken = default
    );
}
