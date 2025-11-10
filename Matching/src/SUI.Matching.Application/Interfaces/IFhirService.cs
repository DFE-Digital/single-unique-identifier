using SUI.Matching.Application.Common;
using SUI.Matching.Application.Models;

namespace SUI.Matching.Application.Interfaces;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery);
}
