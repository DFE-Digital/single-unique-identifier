using NSubstitute;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Infrastructure.Services.PdsServices;

namespace SUI.Find.Infrastructure.UnitTests.Services.FhirServicesTests;

public class FhirServiceTests : BaseFhirClientTests
{
    private readonly FhirService _fhirService;

    public FhirServiceTests()
    {
        _fhirService = new FhirService(LoggerMock, FhirClientFactoryMock);
    }

    [Fact]
    public async Task ShouldReturnError_IfFhirClientHasError()
    {
        // Arrange
        var searchQuery = new SearchQuery();

        // Act
        var testFhirClient = new TestFhirClientError();
        FhirClientFactoryMock.CreateFhirClient().Returns(testFhirClient);
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ShouldReturnUnmatched_IfNoEntriesFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientUnmatched();
        FhirClientFactoryMock.CreateFhirClient().Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SearchResult.ResultType.Unmatched, result.Value?.Type);
    }

    [Fact]
    public async Task ShouldReturnMatch_WithValues_IfOneEntryFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientSinglePersonMatch();
        FhirClientFactoryMock.CreateFhirClient().Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SearchResult.ResultType.Matched, result.Value?.Type);
        Assert.NotNull(result.Value?.NhsNumber);
        Assert.NotEqual(0m, result.Value?.Score);
    }

    [Fact]
    public async Task ShouldReturnSuccessWithManyMatch_IfMultipleEntriesFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientMultiMatch();
        FhirClientFactoryMock.CreateFhirClient().Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SearchResult.ResultType.MultiMatched, result.Value?.Type);
    }
}
