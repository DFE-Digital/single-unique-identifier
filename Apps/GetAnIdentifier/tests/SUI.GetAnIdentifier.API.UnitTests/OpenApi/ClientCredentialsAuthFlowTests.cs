using SUI.GetAnIdentifier.API.OpenApi;

namespace SUI.GetAnIdentifier.API.UnitTests.OpenApi;

public class ClientCredentialsAuthFlowTests
{
    private const string AccessTokenUrl = "http://accessTokenUrl.co.uk";
    private const string OauthScope = "scope/.default";

    [Fact]
    public void AuthFlow_WhenOauthScopeEnvironmentVariableNotSet_ShouldThrowException()
    {
        Environment.SetEnvironmentVariable("AuthSettings__OauthScope", null);
        Environment.SetEnvironmentVariable("AuthSettings__AccessTokenUrl", AccessTokenUrl);

        Assert.Throws<ArgumentNullException>(() => new ClientCredentialsAuthFlow());
    }

    [Fact]
    public void AuthFlow_WhenAccessTokenUrlEnvironmentVariableNotSet_ShouldThrowException()
    {
        Environment.SetEnvironmentVariable("AuthSettings__OauthScope", OauthScope);
        Environment.SetEnvironmentVariable("AuthSettings__AccessTokenUrl", null);

        Assert.Throws<ArgumentNullException>(() => new ClientCredentialsAuthFlow());
    }

    [Fact]
    public void AuthFlow_WhenEnvironmentVariablesSet_ShouldReturnSuccessfully()
    {
        Environment.SetEnvironmentVariable("AuthSettings__OauthScope", OauthScope);
        Environment.SetEnvironmentVariable("AuthSettings__AccessTokenUrl", AccessTokenUrl);

        var sut = new ClientCredentialsAuthFlow();

        Assert.NotNull(sut);
        Assert.Equal(new Uri(AccessTokenUrl), sut.ClientCredentials.TokenUrl);
        Assert.Equal(OauthScope, sut.ClientCredentials.Scopes.Keys.Single());
    }
}
