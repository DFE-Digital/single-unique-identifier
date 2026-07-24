using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Application.Models.Fhir;

namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IPdsSearchStrategy
{
    int Version { get; }
    OrderedDictionary<string, SearchQuery> BuildQuery(PersonSpecification model);
}
