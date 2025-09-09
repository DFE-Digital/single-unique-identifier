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
        var validate = new PersonSpecificationValidation();
        var validationResult = await validate.ValidateAsync(personSpecification);
        
        var dataQualityTranslator = new PersonDataQualityTranslator();
        var (metRequirements, dataQualityResult) = dataQualityTranslator.Translate(personSpecification, validationResult);
        if (!metRequirements)
        {
            const string validationMessage = "The minimized data requirements for a search weren't met, returning match status 'Error'";
            logger.LogWarning("[Validation] {Message}", validationMessage);
            return new PersonMatchResponse
            {
                Result = new MatchResult(MatchStatus.Error, validationMessage),
                DataQuality = dataQualityResult
            };
        }
        
        // Build FHIR query from PersonSpecification
        var queries = PersonQueryBuilder.CreateQueries(personSpecification);
        // TODO: SearchId and set into Activity baggage for logging.
        
        var matchResponse = new PersonMatchResponse
        {
            Result = new MatchResult(MatchStatus.NoMatch),
            DataQuality = dataQualityResult
        };
        
        foreach (var (queryCode, query) in queries)
        {
            logger.LogInformation("Performing search query ({Query}) against Nhs Fhir API", queryCode);
            var searchResult = await fhirService.PerformSearchAsync(query);
            
            if (!searchResult.IsSuccess)
            {
                logger.LogError("FHIR service returned an error: {ErrorMessage}", searchResult.Error);
                matchResponse.Result = new MatchResult(MatchStatus.Error, "Error: Could not complete search");
                continue;
            }
            
            var resultValue = searchResult.Value;

            matchResponse.Result = resultValue!.Score switch
            {
                >= 0.95m => new MatchResult(resultValue, MatchStatus.Match, resultValue.Score, queryCode),
                >= 0.85m => new MatchResult(resultValue, MatchStatus.PotentialMatch, resultValue.Score, queryCode),
                _ => new MatchResult(resultValue, MatchStatus.NoMatch, resultValue.Score, queryCode)
            };
        }

        return matchResponse;
    }
}