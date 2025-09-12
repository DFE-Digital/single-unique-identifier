using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace SUI.Find.Infrastructure.Fhir;

public interface IFhirClient
{
    Task<Bundle?> SearchAsync<T>(SearchParams searchParams) where T : Resource;

    Resource? LastBodyAsResource { get; }
}

public class FhirClientWrapper(FhirClient sdkClient) : IFhirClient
{
    public Task<Bundle?> SearchAsync<T>(SearchParams searchParams) where T : Resource =>
        sdkClient.SearchAsync<T>(searchParams);

    public Resource? LastBodyAsResource => sdkClient.LastBodyAsResource;
}