using System.IO.Abstractions;
using NSubstitute;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private readonly MockAuthStoreService _sut;
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();

    public MockAuthStoreServiceTests()
    {
        _sut = new MockAuthStoreService(_mockFileSystem);
    }

    [Fact]
    public async Task GetAuthClient_ShouldReturnClient_WhenIdAndSecretMatch()
    {
        // Arrange
        var realFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "auth-clients.json");
        var fileContent = await File.ReadAllTextAsync(realFilePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = await _sut.GetClientByCredentials("LOCAL-AUTHORITY-01", "SUIProject");

        // Assert
        Assert.True(result.Success);
    }
}
