using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace SUI.GetAnIdentifier.API.OpenApi;

public class ClientCredentialsAuthFlow : OpenApiOAuthSecurityFlows
{
    public ClientCredentialsAuthFlow()
    {
        var accessTokenUrl =
            Environment.GetEnvironmentVariable("AuthSettings__AccessTokenUrl")
            ?? throw new ArgumentNullException();
        var scope =
            Environment.GetEnvironmentVariable("AuthSettings__OauthScope")
            ?? throw new ArgumentNullException();

        ClientCredentials = new OpenApiOAuthFlow
        {
            TokenUrl = new Uri(accessTokenUrl),

            Scopes = { { scope, "Default scope defined in the app" } },
        };
    }
}
