using System.IO.Abstractions;
using System.Text.Json;
using NSubstitute;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly MockAuthStoreService _sut;
    private readonly string _realStoreFilePath;
    private static readonly string[] ExpectedScopes =
    [
        "match-record.read",
        "find-record.read",
        "find-record.write",
        "fetch-record.read",
        "fetch-record.write",
        "work-item.read",
        "work-item.write",
    ];

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

    [Fact]
    public async Task GetScopesByClientId_WithValidClientId_ShouldReturnScopes()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = _sut.GetScopesByClientId("LOCAL-AUTHORITY-01");

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(ExpectedScopes, result, strict: true);
    }

    [Fact]
    public async Task GetScopesByClientId_WithInvalidClientId_ShouldThrowException()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() =>
            _sut.GetScopesByClientId("invalid-client-id")
        );
    }

    [Fact]
    public async Task GetOrganisationIdForClientId_WithValidClientId_ShouldReturnOrganisationId()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = _sut.GetOrganisationIdForClientId("LOCAL-AUTHORITY-01");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LOCAL-AUTHORITY-01", result);
    }

    [Fact]
    public async Task GetOrganisationByClientId_WithInvalidClientId_ShouldThrowException()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(() =>
            _sut.GetOrganisationIdForClientId("invalid-client-id")
        );
    }
}
