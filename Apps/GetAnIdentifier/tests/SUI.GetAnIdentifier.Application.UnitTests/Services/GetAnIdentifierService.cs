using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.GetAnIdentifier.Application.Enum;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Application.Models.Fhir;
using SUI.GetAnIdentifier.Application.Services;

namespace SUI.GetAnIdentifier.Application.UnitTests.Services;

public class MatchPersonAsyncTests
{
    private readonly GetAnIdentifierService _sut;
    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();

    public MatchPersonAsyncTests()
    {
        var logger = Substitute.For<ILogger<GetAnIdentifierService>>();

        _sut = new GetAnIdentifierService(logger, _fhirService);
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
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

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
            .PerformSearchAsync(
                Arg.Any<SearchQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Application.Models.Result<SearchResult>.Fail("Simulated FHIR service error"));

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

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

        _fhirService
            .PerformSearchAsync(
                Arg.Any<SearchQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Application.Models.Result<SearchResult>.Ok(
                    new SearchResult { Type = SearchResult.ResultType.Unmatched }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnIdentifierAndGeneralPractitioner_WhenExactMatchFound()
    {
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _fhirService
            .PerformSearchAsync(
                Arg.Any<SearchQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Application.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.98m,
                        NhsNumber = "9876543210",
                        GeneralPractitioner = ["B81606"],
                    }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

        // Assert
        var match = Assert.IsType<GetAnIdentifierResult>(result.Value);
        Assert.Equal("9876543210", match.PersonId.Value);
        Assert.Equal(["B81606"], match.GeneralPractitioner);
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

        _fhirService
            .PerformSearchAsync(
                Arg.Any<SearchQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Application.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.94m,
                        NhsNumber = "9876543210",
                    }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenNhsNumberFailsToParse()
    {
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _fhirService
            .PerformSearchAsync(
                Arg.Any<SearchQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Application.Models.Result<SearchResult>.Ok(
                    new SearchResult
                    {
                        Type = SearchResult.ResultType.Matched,
                        Score = 0.98m,
                        NhsNumber = "INVALID-NHS-NUMBER",
                    }
                )
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_IfExceptionIsThrownInSearch()
    {
        // Edge case test
        // Arrange
        var personSpecification = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };

        _fhirService
            .PerformSearchAsync(
                Arg.Any<SearchQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns<Task<Application.Models.Result<SearchResult>>>(_ =>
                throw new Exception("Simulated exception")
            );

        // Act
        var result = await _sut.MatchPersonAsync(personSpecification, ct: CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }
}
