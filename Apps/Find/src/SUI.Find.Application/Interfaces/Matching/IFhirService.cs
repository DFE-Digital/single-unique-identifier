using SUI.Find.Application.Models.Fhir;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces.Matching;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery);
}
