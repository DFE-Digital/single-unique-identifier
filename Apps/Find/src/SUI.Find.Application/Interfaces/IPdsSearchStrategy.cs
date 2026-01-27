using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.Interfaces;

public interface IPdsSearchStrategy
{
    int Version { get; }
    OrderedDictionary<string, SearchQuery> BuildQuery(PersonSpecification model);
}
