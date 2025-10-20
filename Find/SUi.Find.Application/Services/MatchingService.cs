using Microsoft.Extensions.Logging;
using SUi.Find.Application.Builders;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUi.Find.Application.Validation;
using SUI.Find.Domain.Constants;
using SUI.Find.Domain.Enums;

namespace SUi.Find.Application.Services;

/// <summary>
/// Application code class for validating business logic, forming request for FhirService and sending, then processing results.
/// </summary>
public class MatchingService(
    ILogger<MatchingService> logger,
    IFhirService fhirService,
    ISearchIdService searchIdService)
    : IMatchingService
{
    // do we need audit logger?
    // need to add logging to all methods
    public async Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification)
    {
        var (metRequirements, dataQualityResult) = await CheckDataQuality(personSpecification);
        if (!metRequirements)
            return BuildValidationErrorResponse(dataQualityResult,
                "The minimized data requirements for a search weren't met, returning match status 'Error'");

        // Build FHIR query from PersonSpecification
        var queries = PersonQueryBuilder.CreateQueries(personSpecification);
        CreateAndSetSearchId(personSpecification);

        var bestResult = await FindBestMatchResultAsync(queries);

        return new PersonMatchResponse
        {
            Result = bestResult,
            DataQuality = dataQualityResult
        };
    }

    private async Task<MatchResult> FindBestMatchResultAsync(OrderedDictionary<string, SearchQuery> queries)
    {
        MatchResult? best = null;

        foreach (var (queryCode, query) in queries)
        {
            logger.LogInformation("Performing search query ({Query}) against Nhs Fhir API", queryCode);
            var searchResult = await fhirService.PerformSearchAsync(query);

            if (!searchResult.IsSuccess)
            {
                logger.LogError("FHIR service returned an error: {ErrorMessage}", searchResult.Error);
                var errorResult = MatchResult.Error("Error: Could not complete search");

                if (errorResult.IsBetterThan(best))
                    best = errorResult;

                continue;
            }

            var current = MapSearchResult(searchResult.Value!, queryCode);

            if (current.IsBetterThan(best))
                best = current;

            if (current is { MatchStatus: MatchStatus.Match, Score: >= MatchThresholds.MinMatchThreshold }) break;
        }

        return best ?? MatchResult.NoMatch();
    }

    private static MatchResult MapSearchResult(SearchResult value, string queryCode)
    {
        return value.Type switch
        {
            SearchResult.ResultType.Matched => value.Score switch
            {
                >= MatchThresholds.MinMatchThreshold => MatchResult.Match(value.Score.GetValueOrDefault(), queryCode,
                    value.NhsNumber!),
                >= MatchThresholds.MinPartialMatchThreshold => MatchResult.PotentialMatch(
                    value.Score.GetValueOrDefault(), queryCode, value.NhsNumber!),
                _ => MatchResult.NoMatch()
            },
            SearchResult.ResultType.MultiMatched => MatchResult.ManyMatch(queryCode),
            _ => MatchResult.NoMatch()
        };
    }

    private static async Task<(bool metRequirements, DataQualityResult dataQuality)> CheckDataQuality(
        PersonSpecification personSpecification)
    {
        var validate = new PersonSpecificationValidation();
        var validationResult = await validate.ValidateAsync(personSpecification);

        var dataQualityTranslator = new PersonDataQualityTranslator();
        return dataQualityTranslator.Translate(personSpecification, validationResult);
    }

    private PersonMatchResponse BuildValidationErrorResponse(DataQualityResult dq, string message)
    {
        logger.LogWarning("[Validation] {Message}", message);
        return new PersonMatchResponse
        {
            Result = MatchResult.Error(message),
            DataQuality = dq
        };
    }

    private void CreateAndSetSearchId(PersonSpecification personSpecification)
    {
        var hash = searchIdService.CreatePersonHash(
            personSpecification.Given,
            personSpecification.Family,
            personSpecification.BirthDate,
            personSpecification.Gender,
            personSpecification.AddressPostalCode);
        searchIdService.StoreSearchIdInBaggage(hash);
    }
}