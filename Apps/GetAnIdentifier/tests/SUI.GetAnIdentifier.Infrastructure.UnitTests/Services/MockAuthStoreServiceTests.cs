using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.Extensions;
using SUI.GetAnIdentifier.API.Middleware;

namespace SUI.GetAnIdentifier.Infrastructure.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private const string ClientId = "CLIENT_ID_LOCAL_AUTHORITY_01";
    private const string ExpectedOrganisationId = "LOCAL-AUTHORITY-01";
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly IConfiguration _mockConfiguration = Substitute.For<IConfiguration>();
    private readonly MockAuthStoreService _sut;
    private readonly string _realStoreFilePath;
    private static readonly string[] ExpectedScopes = ["get-an-identifier.read"];

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
    public async Task GetClientById_WithValidClientId_ShouldReturnClientWithCorrectDetails()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = _sut.GetClientById(ClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExpectedOrganisationId, result.OrganisationId);
        Assert.Equivalent(ExpectedScopes, result.AllowedScopes, strict: true);
    }

    [Fact]
    public async Task GetClientById_WithInvalidClientId_ShouldReturnNull()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = _sut.GetClientById("invalid-client-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetClientById_ShouldSupportSensitiveOverridesViaConfiguration()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        const string sensitiveClientId = "SensitiveClientId";

        _mockConfiguration[$"AuthClientCredentials:{ClientId}:NewClientId"]
            .Returns(sensitiveClientId);

        // Act
        var result = _sut.GetClientById(sensitiveClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sensitiveClientId, result.ClientId);
        Assert.Equal(ExpectedOrganisationId, result.OrganisationId);
        Assert.Equivalent(ExpectedScopes, result.AllowedScopes, strict: true);
    }

    [Fact]
    public void LoadStore_WhenFileIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(false);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GetClientById(ClientId));
        Assert.Contains("Auth store file not found at", ex.Message);
    }
}
