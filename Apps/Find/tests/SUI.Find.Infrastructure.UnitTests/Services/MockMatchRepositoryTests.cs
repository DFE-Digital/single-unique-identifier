using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockMatchRepositoryTests
{
    private readonly MockMatchRepository _sut;
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly ILogger<MockMatchRepository> _mockLogger = Substitute.For<
        ILogger<MockMatchRepository>
    >();

    public MockMatchRepositoryTests()
    {
        _sut = new MockMatchRepository(_mockLogger, _mockFileSystem);
    }

    [Fact]
    public async Task MatchPersonAsync_ShouldReturnMatch_WhenPersonExists()
    {
        // Arrange
        var realFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "pds-data.json");
        var fileContent = await File.ReadAllTextAsync(realFilePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        var request = new MatchPersonRequest
        {
            Given = "Seth",
            Family = "Parsons",
            BirthDate = new DateOnly(2010, 11, 11),
        };

        // Act
        var result = await _sut.MatchPersonAsync(request);

        // Assert
        Assert.IsType<MatchFhirResponse.Match>(result);
        Assert.Equal("9434765919", ((MatchFhirResponse.Match)result).NhsNumber);
    }

    [Fact]
    public async Task MatchPersonAsync_ShouldReturnNoMatch_WhenPersonDoesNotExist()
    {
        // Arrange
        var realFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "pds-data.json");
        var fileContent = await File.ReadAllTextAsync(realFilePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        var request = new MatchPersonRequest
        {
            Given = "Jane",
            Family = "Smith",
            BirthDate = new DateOnly(1980, 1, 1),
        };

        // Act
        var result = await _sut.MatchPersonAsync(request);

        // Assert
        Assert.IsType<MatchFhirResponse.NoMatch>(result);
    }

    [Fact]
    public async Task MatchPersonAsync_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        _mockFileSystem
            .File.ReadAllTextAsync(Arg.Any<string>())
            .Throws(new Exception("File error"));

        var request = new MatchPersonRequest
        {
            Given = "Any",
            Family = "Person",
            BirthDate = new DateOnly(2000, 1, 1),
        };

        // Act
        var result = await _sut.MatchPersonAsync(request);

        // Assert
        var error = Assert.IsType<MatchFhirResponse.Error>(result);
        Assert.Equal("An error occurred while processing the match request.", error.ErrorMessage);
    }
}
