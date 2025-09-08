
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUi.Find.Application.Services;

namespace Sui.Find.Application.UnitTests.Services;

public class MatchingServiceTests
{
    /*
    [Fact]
    public async Task Should_Do_Something_Template()
    {
        // Arrange

        // Act

        // Assert
        throw new NotImplementedException();
    }
    */

    private readonly IFhirService _fhirService;
    private readonly MatchingService _matchingService;

    public MatchingServiceTests()
    {
        var logger = Substitute.For<ILogger<MatchingService>>();
        _fhirService = Substitute.For<IFhirService>();
        _matchingService = new MatchingService(logger, _fhirService);
    }
    
    [Fact]
    public async Task ShouldReturn_ValidationError_IfMatchingServiceValidationFails()
    {
        // Arrange
        var personSpec = new PersonSpecification
        {
            Given = "Jon",
            Family = "Smith",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
        _fhirService.PerformSearchAsync().Returns(new FhirSearchResult
        {
            ErrorMessage = "Simulated FHIR service error",
            Type = FhirSearchResult.ResultType.Error
        });

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal("Error", response.Result.MatchStatus);
        Assert.NotNull(response.Result.MatchStatusErrorMessage);
        Assert.NotEmpty(response.Result.MatchStatusErrorMessage);
    }
    
    [Fact]
    public Task ShouldReturn_ValidationError_IfFhirServiceContainsValidationErrors()
    {
        // FhirSearchResult is Fhir return class
        // Arrange

        // Act

        // Assert
        throw new NotImplementedException();
    }
    
    [Fact]
    public Task ShouldReturn_MatchResults_WhenFhirServiceCallIsSuccessful()
    {
        // FhirSearchResult is Fhir return class
        // Arrange

        // Act

        // Assert
        throw new NotImplementedException();
    }
    
    
}