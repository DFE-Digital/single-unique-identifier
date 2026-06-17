using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.Extensions;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private const string ClientId = "CLIENT_ID_LOCAL_AUTHORITY_01";
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly IConfiguration _mockConfiguration = Substitute.For<IConfiguration>();
    private readonly MockAuthStoreService _sut;
    private readonly string _realStoreFilePath;

    public MockAuthStoreServiceTests()
    {
        _mockConfiguration.ReturnsForAll((string?)null);

        _sut = new MockAuthStoreService(_mockFileSystem, _mockConfiguration);
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

    [Theory]
    [InlineData(null, null, ClientId, "SUIProject")]
    [InlineData("SensitiveClientId", null, "SensitiveClientId", "SUIProject")]
    [InlineData(null, "SensitiveClientSecret", ClientId, "SensitiveClientSecret")]
    [InlineData("PrivateClientId", "PrivateClientSecret", "PrivateClientId", "PrivateClientSecret")]
    public async Task GetClientByCredentials_ShouldSupportSensitiveOverridesViaConfiguration(
        string? sensitiveClientId,
        string? sensitiveClientSecret,
        string expectedClientId,
        string expectedClientSecret
    )
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        _mockConfiguration[$"AuthClientCredentials:{ClientId}:NewClientId"] = sensitiveClientId;
        _mockConfiguration[$"AuthClientCredentials:{ClientId}:NewClientSecret"] =
            sensitiveClientSecret;

        // Act
        var result = await _sut.GetClientByCredentials(expectedClientId, expectedClientSecret);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedClientId, result.Value.ClientId);
    }
}
