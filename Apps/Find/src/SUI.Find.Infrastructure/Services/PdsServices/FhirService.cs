using Microsoft.Extensions.Logging;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Interfaces.Fhir;

namespace SUI.Find.Infrastructure.Services.PdsServices;

public class FhirService(ILogger<FhirService> logger, IFhirClientFactory factory) : IFhirService
{
    public Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery)
    {
        throw new NotImplementedException();
    }
}
