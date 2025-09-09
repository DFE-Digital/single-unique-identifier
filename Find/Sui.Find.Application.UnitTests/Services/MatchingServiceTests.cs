
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
        Assert.Equal(MatchStatus.Error, response.Result.MatchStatus);
        Assert.NotNull(response.Result.ErrorMessage);
    }
    
    [Fact]
    public async Task ShouldReturn_Error_IfFhirServiceErrors()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Failure("Simulated FHIR service error"));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.Error, response.Result.MatchStatus);
        Assert.NotNull(response.Result.ErrorMessage);
    }
    
    [Theory]
    [InlineData(0.95)]
    [InlineData(1)]
    public async Task ShouldReturn_MatchResults_WhenFhirServiceScoreIs95OrGreater(decimal score)
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultMatched(score)));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.Match, response.Result.MatchStatus);
        Assert.NotNull(response.Result.NhsNumber);
    }
    
    [Theory]
    [InlineData(0.85)]
    [InlineData(0.90)]
    [InlineData(0.94)]
    public async Task ShouldReturn_PotentialMatchResults_WhenFhirServiceScoreIsBetween85And95(decimal score)
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultMatched(score)));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.PotentialMatch, response.Result.MatchStatus);
    }
    
    [Fact]
    public async Task ShouldReturn_ManyMatchResults_WhenFhirServiceReturnsMultiMatch()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultMultiMatch()));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.ManyMatch, response.Result.MatchStatus);
    }
    
    [Fact]
    public async Task ShouldReturn_NoMatchResults_WhenFhirServiceReturnsUnmatched()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>()).Returns(Result<SearchResult>.Success(GetMockFhirSearchResultUnmatched()));

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.NoMatch, response.Result.MatchStatus);
    }
    
    [Fact]
    public async Task ShouldReturnEarly_IfHighConfidenceMatchFound()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>())
            .Returns(
                // First call returns a high confidence match
                Result<SearchResult>.Success(GetMockFhirSearchResultMatched(0.96m)),
                // Second call would return a low confidence match if it were called
                Result<SearchResult>.Success(GetMockFhirSearchResultMatched(0.50m))
            );

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.Match, response.Result.MatchStatus);
        // Verify that PerformSearchAsync was called only once due to early exit
        await _fhirService.Received(1).PerformSearchAsync(Arg.Any<SearchQuery>());
    }
    
    [Fact]
    public async Task ShouldReturnBestResult_OfPotentialMatch_WhenMultipleQueriesExecuted()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>())
            .Returns(
                Result<SearchResult>.Success(GetMockFhirSearchResultMatched(0.90m)),
                Result<SearchResult>.Success(GetMockFhirSearchResultMatched(0.50m))
            );

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.PotentialMatch, response.Result.MatchStatus);
    }
    
    [Fact]
    public async Task ShouldReturnBestResult_OfManyMatch_WhenMultipleQueriesExecuted()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        
        _fhirService.PerformSearchAsync(Arg.Any<SearchQuery>())
            .Returns(
                Result<SearchResult>.Success(GetMockFhirSearchResultMultiMatch()),
                Result<SearchResult>.Success(GetMockFhirSearchResultMatched(0.50m))
            );

        // Act
        var response = await _matchingService.SearchAsync(personSpec);

        // Assert
        Assert.Equal(MatchStatus.ManyMatch, response.Result.MatchStatus);
    }
    
    private static PersonSpecification CreateMinimalValidPersonSpec()
    {
        return new PersonSpecification
        {
            Given = "Jon",
            Family = "Smith",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
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
    
    private static SearchResult GetMockFhirSearchResultMultiMatch()
    {
        return new SearchResult
        {
            NhsNumber = null,
            Score = 0,
            Type = SearchResult.ResultType.MultiMatched
        };
    }
    
    private static SearchResult GetMockFhirSearchResultUnmatched()
    {
        return new SearchResult
        {
            NhsNumber = null,
            Score = 0,
            Type = SearchResult.ResultType.Unmatched
        };
    }
}