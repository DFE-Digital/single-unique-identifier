using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUi.Find.Application.Models;
using SUI.Find.Infrastructure.Fhir;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.UnitTests.Stubs;
using Task = System.Threading.Tasks.Task;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class FhirServiceTests
{
    private readonly ILogger<FhirService> _logger = Substitute.For<ILogger<FhirService>>();
    private readonly IFhirClient _fhirClient = Substitute.For<IFhirClient>();
    private readonly FhirService _fhirService;
    
    public FhirServiceTests()
    {
        _fhirService = new FhirService(_logger, _fhirClient);
    }

    [Fact]
    public async Task ShouldReturnError_IfFhirClientHasError()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        _fhirClient.SearchAsync<Patient>(Arg.Any<SearchParams>())
            .Returns(Task.FromResult<Bundle?>(null));

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ShouldReturnUnmatched_IfNoEntriesFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        FhirClientBundleSetup(StubFhirBundles.Empty());
        

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SearchResult.ResultType.Unmatched, result.Value?.Type);
    }

    [Fact]
    public async Task ShouldReturnMatch_WithValues_IfOneEntryFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        FhirClientBundleSetup(StubFhirBundles.SinglePatient("123", 1.0m));

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SearchResult.ResultType.Matched, result.Value?.Type);
        Assert.Equal("123", result.Value?.NhsNumber);
        Assert.Equal(1.0m, result.Value?.Score);
    }

    [Fact]
    public async Task ShouldReturnSuccessWithManyMatch_IfMultipleEntriesFound()
    {
        // Arrange
        var searchQuery = new SearchQuery();
        FhirClientBundleSetup(StubFhirBundles.MultiplePatients());

        // Act
        var result = await _fhirService.PerformSearchAsync(searchQuery);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SearchResult.ResultType.MultiMatched, result.Value?.Type);
    }

    private void FhirClientBundleSetup(Resource? resource)
    {
        _fhirClient.SearchAsync<Patient>(Arg.Any<SearchParams>())
            .Returns(Task.FromResult(resource as Bundle));
        _fhirClient.LastBodyAsResource.Returns(resource);
    }
}