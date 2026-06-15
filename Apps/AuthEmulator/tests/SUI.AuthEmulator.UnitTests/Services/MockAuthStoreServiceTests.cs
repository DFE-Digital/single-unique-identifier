using System.IO.Abstractions;
using System.Text.Json;
using NSubstitute;
using SUI.AuthEmulator.Models;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private const string ClientId = "CLIENT-ID_LOCAL-AUTHORITY-01";
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly MockAuthStoreService _sut;
    private readonly string _realStoreFilePath;

    public MockAuthStoreServiceTests()
    {
        _sut = new MockAuthStoreService(_mockFileSystem);
        _realStoreFilePath = Path.Join(
            AppContext.BaseDirectory,
            "Data",
            "auth-clients-inbound.json"
        );
    }

    [Fact]
    public async Task GetClientByCredentials_ShouldReturnClient_WhenIdAndSecretMatch()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = await _sut.GetClientByCredentials(ClientId, "SUIProject");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(ClientId, result.Value.ClientId);
    }

    [Fact]
    public async Task GetClientByCredentials_ShouldReturnFailure_WhenCredentialsAreInvalid()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = await _sut.GetClientByCredentials(ClientId, "WRONG-PASSWORD-XYZ");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unauthorized", result.Error);
        Assert.Null(result.Value);
    }
}
