using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Constants.Matching;
using SUI.Find.Application.Enums.Matching;
using SUI.Find.Application.Factories.PdsSearch;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Validation.Matching;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Services.Matching;

// Assumption: We only want to give NHS Id if we have a confident match
// Pilot 1 returned even if there was a potential match, for this MVP we are tightening that
// requirement to only return on confident match. Simple change if we want to adjust this later.
public class MatchingNhsNumberService(
    ILogger<MatchingNhsNumberService> logger,
    IPdsSearchFactory searchFactory,
    IFhirService fhirService
) : IMatchingNhsNumberService
{
    public async Task<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>> MatchPersonAsync(
        PersonSpecification request,
        CancellationToken ct
    )
    {
        try
        {
            var validationResult = await new PersonSpecificationValidation().ValidateAsync(
                request,
                ct
            );
            var translatedResult = PersonDataQualityTranslator.Translate(request, validationResult);
            if (!translatedResult.hasMetRequirements)
            {
                return translatedResult.dataQualityResult;
            }

            var searchQueries = BuildSearchQueries(request);

            var result = await PerformSearchAsync(searchQueries, ct);

            if (result.MatchStatus is not MatchStatus.Match || result.NhsNumber is null)
            {
                return new NotFound();
            }

            var nhsPersonId = NhsPersonId.Create(result.NhsNumber);
            if (nhsPersonId is not { Success: true, Value: not null })
            {
                logger.LogError(
                    "Failed to create NhsPersonId from NHS number: {NhsNumber}",
                    result.NhsNumber
                );
                return new Error();
            }

            return nhsPersonId.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);
            return new Error();
        }
    }

    private OrderedDictionary<string, SearchQuery> BuildSearchQueries(PersonSpecification request)
    {
        // Version number could come from config if we need to support multiple versions
        var searchStrategy = searchFactory.GetVersion(1);
        return searchStrategy.BuildQuery(request);
    }

    private async Task<MatchResult> PerformSearchAsync(
        OrderedDictionary<string, SearchQuery> searchQueries,
        CancellationToken ct
    )
    {
        var currentMatchResult = MatchResult.NoMatch();
        // We want to keep it synchronous so we can stop as soon as we get a confident match
        foreach (var query in searchQueries)
        {
            var result = await fhirService.PerformSearchAsync(query.Value, ct);

            if (!result.Success)
            {
                currentMatchResult = MatchResult.Error(result.Error ?? "Unknown error");
                logger.LogError(
                    "FHIR service returned an error for query {QueryCode}: {ErrorMessage}",
                    query.Key,
                    result.Error
                );
                continue;
            }

            if (result.Value is not null)
            {
                var mappedResult = MapSearchResult(result.Value, query.Key);
                if (mappedResult.IsBetterThan(currentMatchResult))
                {
                    currentMatchResult = mappedResult;
                }
            }
        }

        logger.LogInformation("Match result: {MatchResult}", currentMatchResult);

        return currentMatchResult;
    }

    private static MatchResult MapSearchResult(SearchResult value, string queryCode)
    {
        return value.Type switch
        {
            SearchResult.ResultType.Matched => value.Score switch
            {
                >= MatchScoreConstants.MinMatchThreshold => MatchResult.Match(
                    value.Score.GetValueOrDefault(),
                    queryCode,
                    value.NhsNumber!
                ),
                >= MatchScoreConstants.MinPartialMatchThreshold => MatchResult.PotentialMatch(
                    value.Score.GetValueOrDefault(),
                    queryCode,
                    value.NhsNumber!
                ),
                _ => MatchResult.NoMatch(),
            },
            SearchResult.ResultType.MultiMatched => MatchResult.ManyMatch(queryCode),
            _ => MatchResult.NoMatch(),
        };
    }
}
