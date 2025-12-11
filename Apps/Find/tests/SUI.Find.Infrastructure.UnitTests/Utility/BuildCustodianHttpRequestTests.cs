using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.UnitTests.Utility;

public class BuildCustodianHttpRequestTests
{

    private readonly BuildCustodianHttpRequest _sut = new();
    private const string DefaultOrgId = "test-org-123";
    private const string EncryptedPersonId = "1234567890123456";

    private static ProviderDefinition MockProvider(
        string method = "GET",
        string url = "https://api.example.com/records",
        string personIdPosition = "query",
        string? bodyTemplate = null
    )
    {
        return new ProviderDefinition
        {
            OrgId = DefaultOrgId,
            Connection = new ConnectionDefinition
            {
                Method = method,
                Url = url,
                PersonIdPosition = personIdPosition,
                BodyTemplateJson = bodyTemplate,
            },
        };
    }

    [Theory]
    [InlineData("get", "GET")]
    [InlineData("Post", "POST")]
    [InlineData("pUt", "PUT")]
    [InlineData("PaTcH", "PATCH")]
    [InlineData("delete", "DELETE")]
    public void BuildHttpRequest_ReturnConfiguredHttpRequest(
        string inputMethod,
        string expectedMethod
    )
    {
        // Arrange
        var provider = MockProvider(method: inputMethod);

        // Act
        var request = _sut.BuildHttpRequest(provider, EncryptedPersonId, null);
        // Assert
        Assert.Equal(expectedMethod, request.Method.Method);
        Assert.Contains("orgId", request.Headers.ToString());
        Assert.Equal(DefaultOrgId, request.Headers.GetValues("orgId").Single());
        Assert.Equal(
            $"https://api.example.com/records?personId={Uri.EscapeDataString(EncryptedPersonId)}",
            request.RequestUri!.ToString()
        );
    }

    [Fact]
    public void BuildHttpRequest_ShouldMapPersonIdToUrl()
    {
        // Arrange
        var url = "https://api.example.com/records?{personId}";
        var provider = MockProvider(personIdPosition: "path", url: url);

        // Act
        var request = _sut.BuildHttpRequest(provider, EncryptedPersonId, null);

        // Assert
        Assert.Equal(
            $"https://api.example.com/records?{Uri.EscapeDataString(EncryptedPersonId)}",
            request?.RequestUri?.ToString()
        );
    }

    [Fact]
    public void BuildHttpRequest_ShouldContainAuthorizationHeaderWhenTokenPresent()
    {
        // Arrange
        var token = "secret-test-token";
        var provider = MockProvider();

        // Act
        var request = _sut.BuildHttpRequest(
            provider,
            EncryptedPersonId,
            token
        );

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(token, request.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildHttpRequest_ShouldNotAddAuthorizationHeaderWhenTokenMissing(string? token)
    {
        // Arrange
        var provider = MockProvider();

        // Act
        var request = _sut.BuildHttpRequest(
            provider,
            EncryptedPersonId,
            token
        );

        // Assert
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public void BuildHttpRequest_Should_Add_PersonId_Header_When_Position_Is_Header()
    {
        // Arrange
        var provider = MockProvider(personIdPosition: "header");

        // Act
        var request = _sut.BuildHttpRequest(provider, EncryptedPersonId, null);

        // Assert
        Assert.Contains("personId", request.Headers.ToString());
        Assert.Equal(EncryptedPersonId, request.Headers.GetValues("personId").Single());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public void BuildHttpRequest_ShouldNotBuildBodyForNotAllowedMethods(string method)
    {
        // Arrange
        var provider = MockProvider(
            method: method,
            personIdPosition: "body",
            bodyTemplate: "{\"id\":\"{personId}\"}"
        );

        // Act
        var request = _sut.BuildHttpRequest(provider, EncryptedPersonId, null);

        // Assert
        Assert.Null(request.Content);
    }

    [Fact]
    public async Task BuildHttpRequest_ShouldUseBodyTemplateWithAllowedMethod()
    {
        // Arrange
        var template = "{\"records\": { \"id\": \"{personId}\" }}";
        var provider = MockProvider(
            method: "POST",
            personIdPosition: "query",
            bodyTemplate: template
        );

        // Act
        var request = _sut.BuildHttpRequest(provider, EncryptedPersonId, null);

        // Assert
        Assert.NotNull(request.Content);
        Assert.Equal(
            $"{{\"records\": {{ \"id\": \"{EncryptedPersonId}\" }}}}",
            await request.Content.ReadAsStringAsync()
        );
    }
}
