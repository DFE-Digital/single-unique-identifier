using SUI.GetAnIdentifier.Application.Models.Fhir;
using SUI.GetAnIdentifier.Domain.Models;

namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery, CancellationToken ct);
}
