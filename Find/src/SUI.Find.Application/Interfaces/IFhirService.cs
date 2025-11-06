using SUI.Find.Application.Common;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IFhirService
{
    Task<Result<SearchResult>> PerformSearchAsync(SearchQuery searchQuery);
}