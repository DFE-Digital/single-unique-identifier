using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

/// <summary>
/// Simulated match service for testing and development purposes.
/// </summary>
/// <param name="logger"></param>
/// <param name="fileSystem"></param>
public class MockMatchRepository(ILogger<MockMatchRepository> logger, IFileSystem fileSystem)
    : IMatchRepository
{
    public async Task<MatchFhirResponse> MatchPersonAsync(MatchPersonRequest request)
    {
        try
        {
            // Simulate a mock match response
            // TODO: Read Mock file
            var mockDataPath = fileSystem.Path.Combine("Data", "pds-data.json");
            logger.LogInformation("Reading mock data from {MockDataPath}", mockDataPath);
            var mockData = await fileSystem.File.ReadAllTextAsync(mockDataPath);
            var mockPersons = JsonSerializer.Deserialize<MockPdsStore>(
                mockData,
                JsonSerializerOptions.Web
            );

            // Find based on Given, Family and Birthdate - case insensitive
            var match = mockPersons?.People.FirstOrDefault(person =>
                string.Equals(person.Given, request.Given, StringComparison.OrdinalIgnoreCase)
                && string.Equals(person.Family, request.Family, StringComparison.OrdinalIgnoreCase)
                && request.BirthDate.HasValue
                && Math.Abs(
                    ((person.BirthDate.Year - request.BirthDate.Value.Year) * 12)
                        + (person.BirthDate.Month - request.BirthDate.Value.Month)
                ) <= 6
            );

            if (match == null)
            {
                logger.LogInformation("No match found");
                return new MatchFhirResponse.NoMatch();
            }

            logger.LogInformation("Match found");

            return new MatchFhirResponse.Match(match.NhsNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while matching person");
            return new MatchFhirResponse.Error(
                "An error occurred while processing the match request."
            );
        }
    }
}
