using SUI.Find.Application.Models.Fhir;
using SUI.Find.Domain.Models;

namespace SUI.Find.Infrastructure.Interfaces.Fhir;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery);
}
