using SUi.Find.Application.Common;
using SUi.Find.Application.Models;

namespace SUi.Find.Application.Interfaces;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery);
}