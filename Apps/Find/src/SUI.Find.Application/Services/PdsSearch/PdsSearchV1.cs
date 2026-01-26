using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.Services.PdsSearch;

/// <summary>
/// V1 is taken from Strategy 4 version 1 of the Pilot 1 PDS search strategies
/// </summary>
public class PdsSearchV1 : IPdsSearchStrategy
{
    public int Version => 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        throw new NotImplementedException();
    }
}
