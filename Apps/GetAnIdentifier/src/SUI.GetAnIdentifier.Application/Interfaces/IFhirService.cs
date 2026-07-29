using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Application.Models.Fhir;

namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery, CancellationToken ct);
}
