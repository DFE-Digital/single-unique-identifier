using System.IO.Abstractions;
using NSubstitute;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class MockCustodianServiceTests
{
    private readonly IFileSystem _mockFileSystem = Substitute.For<IFileSystem>();
    private readonly MockCustodianService _sut;

    public MockCustodianServiceTests()
    {
        _sut = new MockCustodianService(_mockFileSystem);
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
            p is { OrgId: "LOCAL-AUTHORITY-01", RecordType: "local-authority" }
        );
        Assert.NotNull(la);
        Assert.Equal("Example Local Authority", la.OrgName);
        Assert.Equal("LOCAL_AUTHORITY", la.OrgType);
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
        Assert.NotNull(la.Encryption);
        Assert.Equal("AES-256-ECB", la.Encryption.Algorithm);

        // Check an education record with a body template
        var edu = providers.FirstOrDefault(p =>
            p.OrgId == "EDUCATION-01" && p.RecordType == "education"
        );
        Assert.NotNull(edu);
        Assert.Equal("POST", edu.Connection.Method);
        Assert.Equal("body", edu.Connection.PersonIdPosition);
        Assert.NotNull(edu.Connection.BodyTemplateJson);
    }
}
