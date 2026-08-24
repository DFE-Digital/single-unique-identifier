using NSubstitute;
using SUI.GetAnIdentifier.Application.Models.Fhir;
using SUI.GetAnIdentifier.Infrastructure.Services;

namespace SUI.GetAnIdentifier.Infrastructure.UnitTests.Services;

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
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenFhirOperationExceptionIsThrown()
    {
        // Arrange
        var searchQuery = new SearchQuery();

        // Act
        var testFhirClient = new TestFhirClientOperationOutcomeError(); // Assumes this mocks a FhirOperationException throw
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("PDS API Error", result.Error);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenTaskCanceledExceptionIsThrown()
    {
        // Arrange
        var searchQuery = new SearchQuery();

        // Act
        var testFhirClient = new TestFhirClientTimeout(); // Assumes this mocks a TaskCanceledException throw
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("PDS API Timeout", result.Error);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenHttpRequestExceptionIsThrown()
    {
        // Arrange
        var searchQuery = new SearchQuery();

        // Act
        var testFhirClient = new TestFhirClientNetworkError(); // Assumes this mocks an HttpRequestException throw
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("PDS API Network Error", result.Error);
    }

    [Fact]
    public async Task ShouldReturnUnmatched_IfNoEntriesFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientUnmatched();
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

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
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SearchResult.ResultType.Matched, result.Value?.Type);
        Assert.NotNull(result.Value?.NhsNumber);
        Assert.NotEqual(0m, result.Value?.Score);
        Assert.Equal(["B81606"], result.Value?.GeneralPractitioner);
    }

    [Fact]
    public async Task ShouldReturnEmptyGeneralPractitioner_WhenPatientHasNoRegisteredPractice()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientSinglePersonMatch(includeGeneralPractitioner: false);
        FhirClientFactoryMock.CreateFhirClientAsync().Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(
            searchQuery,
            string.Empty,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Value!.GeneralPractitioner);
    }

    [Fact]
    public async Task ShouldReturnSuccessWithManyMatch_IfMultipleEntriesFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientMultiMatch();
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SearchResult.ResultType.MultiMatched, result.Value?.Type);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenTheBundleResourceIdIsNull()
    {
        // Edge case test
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientNoResourceId();
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenTheBundleSearchIsNull()
    {
        // Edge case test
        // Arrange
        var searchQuery = new SearchQuery();
        var testFhirClient = new TestFhirClientEntryComponentSearchNull();
        FhirClientFactoryMock
            .CreateFhirClientAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(testFhirClient);

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery, ct: CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }
}
