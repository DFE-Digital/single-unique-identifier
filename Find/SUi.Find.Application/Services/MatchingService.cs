using Microsoft.Extensions.Logging;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;

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
        // TODO: Validation of personSpecification here.
        var validate = new PersonSpecificationValidation();
        var validationResult = await validate.ValidateAsync(personSpecification);
        
        // TODO: Create quality result.
        // TODO: Check if minimum data for search is present.
        // TODO: Create Query for FHIR service.
        // TODO: SearchId set into Activity baggage for logging.
        
        logger.LogInformation("Start searching. TODO");
        
        var fhirSearchResult = await fhirService.PerformSearchAsync();
        var matchResponse = new PersonMatchResponse
        {
            Result = new PersonMatchResponse.MatchResult
            {
                MatchStatus = fhirSearchResult.Type.ToString(),
                MatchStatusErrorMessage = fhirSearchResult.ErrorMessage,
                NhsNumber = fhirSearchResult.NhsNumber,
                ProcessStage = "",
                Score = fhirSearchResult.Score
            },
            DataQuality = new PersonMatchResponse.MatchDataQuality
            {
                Given = "",
                Family = "",
                Birthdate = "",
                AddressPostalCode = "",
                Phone = "",
                Email = "",
                Gender = ""
            }
        };
        return matchResponse;
    }
}