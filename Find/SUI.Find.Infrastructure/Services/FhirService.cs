using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;

namespace SUI.Find.Infrastructure.Services;

/// <summary>
/// Infrastructure code class for calling FHIR endpoint.
/// </summary>
public class FhirService : IFhirService
{
    public Task<FhirSearchResult> PerformSearchAsync()
    {
        // TODO: Implement FHIR search logic here. Keep as thin as possible.
        // Ideally only HTTP calls and serialization/deserialization from the SDK respectively.
        throw new NotImplementedException();
    }
}