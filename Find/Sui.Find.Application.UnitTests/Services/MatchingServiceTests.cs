
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUi.Find.Application;
using SUi.Find.Application.Common;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUi.Find.Application.Services;

namespace Sui.Find.Application.UnitTests.Services;

public class MatchingServiceTests
{
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
            Family = "",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultMatched(0)));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(nameof(MatchStatus.Error), response.Result.Status);
    }
    
    [Fact]
    public async Task ShouldReturn_Error_IfFhirServiceErrors()
    {
        // Arrange
        var personSpec = new PersonSpecification
        {
            Given = "Jon",
            Family = "Smith",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Failure("Simulated FHIR service error"));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(nameof(MatchStatus.Error), response.Result.Status);
    }
    
    [Theory]
    [InlineData(0.95)]
    [InlineData(1)]
    public async Task ShouldReturn_MatchResults_WhenFhirServiceScoreIs95OrGreater(decimal score)
    {
        // Arrange
        var personSpec = new PersonSpecification
        {
            Given = "Jon",
            Family = "Smith",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultMatched(score)));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(nameof(MatchStatus.Match), response.Result.Status);
    }
    
    [Theory]
    [InlineData(0.85)]
    [InlineData(0.90)]
    [InlineData(0.94)]
    public async Task ShouldReturn_PotentialMatchResults_WhenFhirServiceScoreIsBetween85And95(decimal score)
    {
        // Arrange
        var personSpec = new PersonSpecification
        {
            Given = "Jon",
            Family = "Smith",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultMatched(score)));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(nameof(MatchStatus.PotentialMatch), response.Result.Status);
    }
    
    private static SearchResult GetMockFhirSearchResultMatched(decimal score)
    {
        return new SearchResult
        {
            NhsNumber = "1234567890",
            Score = score,
            Type = SearchResult.ResultType.Matched
        };
    }
}