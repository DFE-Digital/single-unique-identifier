using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockCustodianServiceTests
{
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly IConfiguration _mockConfiguration = Substitute.For<IConfiguration>();
    private readonly MockCustodianService _sut;

    public MockCustodianServiceTests()
    {
        _sut = new MockCustodianService(_mockFileSystem, _mockConfiguration);
    }

    [Fact]
    public async Task GetCustodiansAsync_MapsOrgDirectoryJsonToProviderDefinitions()
    {
        // Arrange: load the actual org-directory.json from Data
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "org-directory.json");
        var fileContent = await File.ReadAllTextAsync(filePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        // Act: get all custodians
        var providers = await _sut.GetCustodiansAsync();

        // Assert: check a few known mappings
        var la = providers.FirstOrDefault(p =>
            p is { OrgId: "LOCAL-AUTHORITY-01", RecordType: "childrens-services.details" }
        );
        Assert.NotNull(la);
        Assert.Equal("Example Local Authority", la.OrgName);
        Assert.Equal("LOCAL_AUTHORITY", la.OrgType);
        // Connection
        Assert.Equal("GET", la.Connection.Method);
        Assert.Equal(
            "http://localhost:7082/v1/local-authority/manifest/{personId}?recordType=local-authority",
            la.Connection.Url
        );
        Assert.Equal("path", la.Connection.PersonIdPosition);
        Assert.NotNull(la.Connection.Auth);
        Assert.Equal("oauth2_client_credentials", la.Connection.Auth.Type);
        Assert.Equal("http://localhost:7082/v1/auth/token", la.Connection.Auth.TokenUrl);
        Assert.Contains("find-record.read fetch-record.read", la.Connection.Auth.Scopes);
        Assert.Equal("SUI-SERVICE", la.Connection.Auth.ClientId);
        Assert.Equal("SUIProject", la.Connection.Auth.ClientSecret);
        // dsa policy

        Assert.Equal(DateTimeOffset.Parse("2025-11-10T12:00:00Z"), la.DsaPolicy.Version);
        Assert.NotEmpty(la.DsaPolicy.Defaults);
        var defaultRule = la.DsaPolicy.Defaults.First();
        Assert.Equal("allow", defaultRule.Effect);
        Assert.Contains("EXISTENCE", defaultRule.Modes);
        Assert.Contains("childrens-services.details", defaultRule.RecordTypes);
        Assert.Contains("POLICE", defaultRule.DestOrgTypes);
        Assert.Equal(DateTimeOffset.Parse("2025-01-01T00:00:00Z"), defaultRule.ValidFrom);

        var police = providers.FirstOrDefault(p => p.OrgId == "POLICE-01");
        Assert.NotNull(police);
        Assert.NotEmpty(police.DsaPolicy.Exceptions);

        var exceptionRule = police.DsaPolicy.Exceptions.First();
        Assert.Contains("LOCAL-AUTHORITY-01", exceptionRule.DestOrgIds);
        Assert.Equal("Timeboxed multi-agency safeguarding operation.", exceptionRule.Reason);
        Assert.Equal(DateTimeOffset.Parse("2025-12-01T00:00:00Z"), exceptionRule.ValidFrom);
        Assert.Equal(DateTimeOffset.Parse("2026-03-01T00:00:00Z"), exceptionRule.ValidUntil);

        // encryption
        Assert.NotNull(la.Encryption);
        Assert.Equal("AES-256-ECB", la.Encryption.Algorithm);

        // Check an education record with a body template
        var edu = providers.FirstOrDefault(p =>
            p.OrgId == "EDUCATION-01" && p.RecordType == "education.details"
        );
        Assert.NotNull(edu);
        Assert.Equal("POST", edu.Connection.Method);
        Assert.Equal("body", edu.Connection.PersonIdPosition);
        Assert.NotNull(edu.Connection.BodyTemplateJson);
    }

    [Fact]
    public async Task GetCustodianAsync_ReturnsCustodian_WhenFound()
    {
        // Arrange
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "org-directory.json");
        var fileContent = await File.ReadAllTextAsync(filePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        var targetOrgId = "LOCAL-AUTHORITY-01";

        // Act
        var result = await _sut.GetCustodianAsync(targetOrgId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(targetOrgId, result?.Value?.OrgId);
        Assert.Equal("Example Local Authority", result?.Value?.OrgName);
    }

    [Fact]
    public async Task GetCustodianAsync_ThrowsKeyNotFound_WhenNotFound()
    {
        // Arrange
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "org-directory.json");
        var fileContent = await File.ReadAllTextAsync(filePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        var targetOrgId = "NON-EXISTENT-ORG-ID";

        // Act
        var result = await _sut.GetCustodianAsync(targetOrgId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(targetOrgId, result.Error);
    }

    [Fact]
    public async Task GetCustodiansAsync_DoesReplace_StubCustodiansBaseUrl_TextToken()
    {
        // Arrange
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "org-directory.json");
        var fileContent = await File.ReadAllTextAsync(filePath);
        _mockFileSystem.File.ReadAllTextAsync(Arg.Any<string>()).Returns(fileContent);

        _mockConfiguration["StubCustodiansBaseUrl"].Returns("https://example123.com");

        // Act
        var providers = await _sut.GetCustodiansAsync();

        // Assert
        var provider = providers.FirstOrDefault(p => p.OrgId == "EDUCATION-01");
        Assert.NotNull(provider);
        Assert.Equal("Example Education Custodian", provider.OrgName);

        Assert.Equal("https://example123.com/v1/education/manifest", provider.Connection.Url);
        Assert.NotNull(provider.Connection.Auth);
        Assert.Equal("https://example123.com/v1/auth/token", provider.Connection.Auth.TokenUrl);
    }
}
