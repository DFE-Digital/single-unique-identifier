using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.OpenApi.Models;

namespace SUI.Find.FindApi.OpenApi;

// Defines the oauth2_clientCredentials flow and explicitly adds the match-record.read scope
[ExcludeFromCodeCoverage(
    Justification = "OpenAPI security flow does not contain any logic to be tested."
)]
public class MatchRecordClientCredentialsFlow : OpenApiOAuthSecurityFlows
{
    public MatchRecordClientCredentialsFlow()
    {
        ClientCredentials = new OpenApiOAuthFlow
        {
            TokenUrl = new Uri("/api/v1/auth/token", UriKind.Relative),
            Scopes =
            {
                { "match-record.read", "Obtain the Single Unique Identifier for a person." },
            },
        };
    }
}
