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
        // TODO: Create quality result.
        // TODO: Check if minimum data for search is present.
        // TODO: Create Query for FHIR service.
        // TODO: SearchId set into Activity baggage for logging.
        
        var fhirSearchResult = await fhirService.PerformSearchAsync();
        var matchResponse = new PersonMatchResponse
        {
            Result = new PersonMatchResponse.MatchResult
            {
                MatchStatus = fhirSearchResult.Type.ToString(),
                MatchStatusErrorMessage = fhirSearchResult.ErrorMessage,
                NhsNumber = fhirSearchResult.NhsNumber,
                ProcessStage = null,
                Score = fhirSearchResult.Score
            },
            DataQuality = new PersonMatchResponse.MatchDataQuality
            {
                Given = null,
                Family = null,
                Birthdate = null,
                AddressPostalCode = null,
                Phone = null,
                Email = null,
                Gender = null
            }
        };
        return matchResponse;
    }
}