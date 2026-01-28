using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Enums.Matching;
using SUI.Find.Application.Factories.PdsSearch;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models.Fhir;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services.Matching;
using SUI.Find.Application.Services.PdsSearch;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.ApplicationTests.Services.MatchingTests.MatchingServiceTests;

public class MatchPersonAsyncTests
{
    private readonly MatchingService _sut;
    private readonly IPdsSearchFactory _pdsSearchFactory = Substitute.For<IPdsSearchFactory>();
    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();

    public MatchPersonAsyncTests()
    {
        var logger = Substitute.For<ILogger<MatchingService>>();

        _sut = new MatchingService(logger, _pdsSearchFactory, _fhirService);
    }

    [Fact]
    public async Task ShouldReturnDataQualityResult_WhenValidationFails()
    {
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, CancellationToken.None);

        // Assert
        Assert.IsType<DataQualityResult>(result.Value);
        var dataQualityResult = result.AsT1;
        Assert.Equal(QualityType.NotProvided, dataQualityResult.Given);
        Assert.Equal(QualityType.Valid, dataQualityResult.Family);
        Assert.Equal(QualityType.Valid, dataQualityResult.BirthDate);
    }

    [Fact]
    public async Task ShouldReturnError_WhenFhirServiceReturnsError()
    {
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _fhirService
            .PerformSearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<SearchResult>.Fail("Simulated FHIR service error"));

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenNoMatchesFound()
    {
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _pdsSearchFactory.GetVersion(Arg.Any<int>()).Returns(new PdsSearchV1());

        _fhirService
            .PerformSearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(
                Domain.Models.Result<SearchResult>.Ok(
                    new SearchResult { Type = SearchResult.ResultType.Unmatched }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnNhsPersonId_WhenExactMatchFound()
    {
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _pdsSearchFactory.GetVersion(Arg.Any<int>()).Returns(new PdsSearchV1());

        _fhirService
            .PerformSearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(
                Domain.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.98m,
                        NhsNumber = "9876543210",
                    }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, CancellationToken.None);

        // Assert
        Assert.IsType<NhsPersonId>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenOnlyPartialMatchesFound()
    {
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _pdsSearchFactory.GetVersion(Arg.Any<int>()).Returns(new PdsSearchV1());

        _fhirService
            .PerformSearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(
                Domain.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.94m,
                        NhsNumber = "9876543210",
                    }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnFound_WhenAtLeastOneQueryReturnsConfidentMatch()
    {
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _pdsSearchFactory.GetVersion(Arg.Any<int>()).Returns(new PdsSearchV1());

        _fhirService
            .PerformSearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(
                Domain.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.85m,
                        NhsNumber = "9449305552",
                    }
                ),
                Domain.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.98m,
                        NhsNumber = "9876543210",
                    }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, CancellationToken.None);

        // Assert
        Assert.IsType<NhsPersonId>(result.Value);
    }
}
