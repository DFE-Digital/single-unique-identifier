using Microsoft.Extensions.Logging;
using SUi.Find.Application.Builders;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUi.Find.Application.Validation;

namespace SUi.Find.Application.Services;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification);
}

/// <summary>
/// Application code class for validating business logic, forming request for FhirService and sending, then processing results.
/// </summary>
public class MatchingService(ILogger<MatchingService> logger, IFhirService fhirService) : IMatchingService
{
    public async Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification)
    {
        var (metRequirements, dataQualityResult) = await CheckDataQuality(personSpecification);
        if (!metRequirements)
        {
            return BuildErrorResponse(dataQualityResult,
                "The minimized data requirements for a search weren't met, returning match status 'Error'");
        }

        // Build FHIR query from PersonSpecification
        var queries = PersonQueryBuilder.CreateQueries(personSpecification);
        // TODO: SearchId and set into Activity baggage for logging.

        var bestResult = await FindBestMatchResultAsync(queries);

        return new PersonMatchResponse
        {
            Result = bestResult,
            DataQuality = dataQualityResult
        };
    }

    private async Task<MatchResult> FindBestMatchResultAsync(OrderedDictionary<string, SearchQuery> queries)
    {
        MatchResult? bestResult = null;
        var bestPriority = -1;
        decimal bestScore = -1;

        foreach (var (queryCode, query) in queries)
        {
            logger.LogInformation("Performing search query ({Query}) against Nhs Fhir API", queryCode);
            var searchResult = await fhirService.PerformSearchAsync(query);

            if (!searchResult.IsSuccess)
            {
                logger.LogError("FHIR service returned an error: {ErrorMessage}", searchResult.Error);
                bestResult = new MatchResult(MatchStatus.Error, "Error: Could not complete search");
                continue; // Proceed to next query if available see if that yields a result
            }

            var resultValue = searchResult.Value!;


            var currentResult = resultValue.Type switch
            {
                SearchResult.ResultType.Matched => resultValue.Score switch
                {
                    >= Constants.MinMatchThreshold => new MatchResult(MatchStatus.Match, resultValue.Score, queryCode,
                        resultValue.NhsNumber),
                    >= Constants.MinPartialMatchThreshold => new MatchResult(MatchStatus.PotentialMatch,
                        resultValue.Score, queryCode,
                        resultValue.NhsNumber),
                    _ => new MatchResult(MatchStatus.NoMatch, resultValue.Score, queryCode)
                },
                SearchResult.ResultType.MultiMatched => new MatchResult(MatchStatus.ManyMatch, null, queryCode),
                _ => new MatchResult(MatchStatus.NoMatch, resultValue.Score, queryCode)
            };

            var currentPriority = GetMatchPriority(currentResult.MatchStatus);

            if (currentPriority > bestPriority ||
                (currentPriority == bestPriority && currentResult.Score > bestScore))
            {
                bestResult = currentResult;
                bestPriority = currentPriority;
                bestScore = currentResult.Score ?? -1;
            }

            if (currentResult is { MatchStatus: MatchStatus.Match, Score: >= Constants.MinMatchThreshold })
                break;
        }

        return bestResult ?? new MatchResult(MatchStatus.NoMatch);
    }


    private static int GetMatchPriority(MatchStatus status)
    {
        return status switch
        {
            MatchStatus.Match => 3,
            MatchStatus.PotentialMatch => 2,
            MatchStatus.ManyMatch => 1,
            MatchStatus.NoMatch => 0,
            _ => -1
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
    
    private PersonMatchResponse BuildErrorResponse(DataQualityResult dq, string message)
    {
        logger.LogWarning("[Validation] {Message}", message);
        return new PersonMatchResponse
        {
            Result = new MatchResult(MatchStatus.Error, message),
            DataQuality = dq
        };
    }
}