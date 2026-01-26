using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.Services.PdsSearch;

/// <summary>
/// V1 is taken from Strategy 4 version 1 of the Pilot 1 PDS search strategies
/// <para>https://github.com/DFE-Digital/SUI_Matcher/blob/main/src/matching-api/Search/SearchStrategy4.cs</para>
/// </summary>
public class PdsSearchV1 : IPdsSearchStrategy
{
    public int Version => 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
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
