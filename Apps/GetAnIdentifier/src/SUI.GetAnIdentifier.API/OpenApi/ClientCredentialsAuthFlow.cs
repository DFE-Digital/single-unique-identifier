using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace SUI.GetAnIdentifier.API.OpenApi;

public class ClientCredentialsAuthFlow : OpenApiOAuthSecurityFlows
{
    public ClientCredentialsAuthFlow()
    {
        var accessTokenUrl =
            Environment.GetEnvironmentVariable("AuthSettings__AccessTokenUrl")
            ?? throw new ArgumentNullException(
                "AuthSettings__AccessTokenUrl",
                "Access token url is required."
            );
        var scope =
            Environment.GetEnvironmentVariable("AuthSettings__OauthScope")
            ?? throw new ArgumentNullException(
                "AuthSettings__OauthScope",
                "OAuth scope is required."
            );

        ClientCredentials = new OpenApiOAuthFlow
        {
            TokenUrl = new Uri(accessTokenUrl),

            Scopes = { { scope, "Default scope defined in the app" } },
        };
    }
}
