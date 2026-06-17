using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.Extensions;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockAuthStoreServiceTests
{
    private const string ClientId = "CLIENT-ID_LOCAL-AUTHORITY-01";
    private const string ExpectedOrganisationId = "LOCAL-AUTHORITY-01";
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly IConfiguration _mockConfiguration = Substitute.For<IConfiguration>();
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
        _mockConfiguration.ReturnsForAll((string?)null);

        _sut = new MockAuthStoreService(_mockFileSystem, _mockConfiguration);
        _realStoreFilePath = Path.Join(
            AppContext.BaseDirectory,
            "Data",
            "auth-clients-inbound.json"
        );
    }

    [Fact]
    public async Task GetScopesByClientId_WithValidClientId_ShouldReturnScopes()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        // Act
        var result = _sut.GetScopesByClientId(ClientId);

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
        var result = _sut.GetOrganisationIdForClientId(ClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExpectedOrganisationId, result);
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

    [Fact]
    public async Task GetOrganisationIdForClientId_AndGetScopesByClientId_ShouldSupportSensitiveOverridesViaConfiguration()
    {
        // Arrange
        var fileContent = await File.ReadAllTextAsync(_realStoreFilePath);
        _mockFileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _mockFileSystem.File.ReadAllText(Arg.Any<string>()).Returns(fileContent);

        const string sensitiveClientId = "SensitiveClientId";

        _mockConfiguration[$"AuthClientCredentials:{ClientId}:NewClientId"] = sensitiveClientId;

        // Act
        var resultOrganisationId = _sut.GetOrganisationIdForClientId(sensitiveClientId);
        var resultScopes = _sut.GetScopesByClientId(sensitiveClientId);

        // Assert
        Assert.Equal(ExpectedOrganisationId, resultOrganisationId);
        Assert.Equivalent(ExpectedScopes, resultScopes, strict: true);
    }
}
