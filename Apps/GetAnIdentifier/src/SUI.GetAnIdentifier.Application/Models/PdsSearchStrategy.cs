using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models.Fhir;
using SUI.GetAnIdentifier.Application.Services;

namespace SUI.GetAnIdentifier.Application.Models;

/// <summary>
/// V1 is taken from Strategy 4 version 1 of the Pilot 1 PDS search strategies
/// <para>https://github.com/DFE-Digital/SUI_Matcher/blob/main/src/matching-api/Search/SearchStrategy4.cs</para>
/// </summary>
public class PdsSearchV1 : IPdsSearchStrategy
{
    public int Version => 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(PersonSpecification model)
    {
        var queryBuilder = new SearchQueryBuilder(model, dobRange: 6, preprocessNames: true);

        queryBuilder.AddNonFuzzyGfd();

        queryBuilder.AddFuzzyGfd();

        queryBuilder.AddFuzzyAll();

        queryBuilder.AddNonFuzzyGfdRange();

        queryBuilder.AddNonFuzzyGfdRangePostcode(usePostcodeWildcard: false);

        queryBuilder.AddFuzzyGfdRange();

        queryBuilder.AddFuzzyGfdRangePostcode();

        return queryBuilder.Build();
    }
}
