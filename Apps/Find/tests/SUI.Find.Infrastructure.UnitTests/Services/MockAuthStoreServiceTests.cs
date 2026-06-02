using System.IO.Abstractions;
using System.Text.Json;
using NSubstitute;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly MockAuthStoreService _sut;
    private readonly string _realStoreFilePath;

    public MockAuthStoreServiceTests()
    {
        _sut = new MockAuthStoreService(_mockFileSystem);
        _realStoreFilePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "auth-clients-inbound.json"
        );
    }

    [Fact]
    public async Task GetAuthStoreAsync_MapsJsonToStoreDefinitionCorrectly()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var store = await _sut.GetAuthStoreAsync();

        // Assert
        Assert.NotNull(store);
        Assert.Equal("https://sandbox.api.example.gov.uk/find-a-record/auth", store.Issuer);
        Assert.Equal("find-a-record-api", store.Audience);
        Assert.Equal(
            "Wc8Kq1ZyR4hNfD0uVx3mS9JpA6eLrT2bG7wQvY5sCjP8kF1nH0tUoMzBiXaEdRl",
            store.SigningKey
        );
        Assert.NotNull(store.Clients);
        Assert.NotEmpty(store.Clients);

        // Verify structure of an active client
        var sampleClient = store.Clients.FirstOrDefault(c => c.ClientId == "LOCAL-AUTHORITY-01");
        Assert.NotNull(sampleClient);
        Assert.True(sampleClient.Enabled);
        Assert.Equal("SUIProject", sampleClient.ClientSecret);
        Assert.Contains("match-record.read", sampleClient.AllowedScopes!);
    }

    [Fact]
    public async Task GetClientByCredentials_ShouldReturnClient_WhenIdAndSecretMatch()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = await _sut.GetClientByCredentials("LOCAL-AUTHORITY-01", "SUIProject");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("LOCAL-AUTHORITY-01", result.Value.ClientId);
    }

    [Fact]
    public async Task GetClientByCredentials_ShouldReturnFailure_WhenCredentialsAreInvalid()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = await _sut.GetClientByCredentials("LOCAL-AUTHORITY-01", "WRONG-PASSWORD-XYZ");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Unauthorized", result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetClientByCredentials_ShouldThrowInvalidOperationException_WhenFileHeaderIsMissingMetadata()
    {
        // Arrange
        var badStore = new AuthStore
        {
            Issuer = "", // Missing
            Audience = "some-audience",
            SigningKey = "some-key",
            DefaultTokenLifetimeMinutes = 60,
        };
        var badJson = JsonSerializer.Serialize(badStore, JsonSerializerOptions.Web);

        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(badJson);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.GetClientByCredentials("LOCAL-AUTHORITY-01", "SUIProject")
        );

        Assert.Equal("Auth store file is missing issuer, audience, or signingKey.", ex.Message);
    }

    [Fact]
    public async Task LoadStore_ShouldThrowInvalidOperationException_WhenFileDoesNotExist()
    {
        // Arrange
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.GetAuthStoreAsync()
        );
        Assert.Contains("Auth store file not found at", ex.Message);
    }

    [Fact]
    public async Task LoadStore_ShouldThrowInvalidOperationException_WhenJsonIsCorrupt()
    {
        // Arrange
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem
            .File.ReadAllText(Arg.Any<string>())
            .Returns("{ invalid-json-payload : true ");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => _sut.GetAuthStoreAsync());
    }
}
