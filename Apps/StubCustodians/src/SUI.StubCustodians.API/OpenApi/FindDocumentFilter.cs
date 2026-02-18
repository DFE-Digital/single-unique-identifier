using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SUI.StubCustodians.API.OpenApi;

[ExcludeFromCodeCoverage(Justification = "OpenAPI documentation")]
public sealed class FindDocumentFilter : IOpenApiDocumentTransformer
{
    private static OpenApiTag Tag(string name, string description, int order) =>
        new()
        {
            Name = name,
            Description = description,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-tag-order"] = new JsonNodeExtension(JsonValue.Create(order)),
            },
        };

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["oauth2_clientCredentials"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri("/api/v1/auth/token", UriKind.Relative),
                    Scopes = new Dictionary<string, string>
                    {
                        { "find-record.read", "Read search status and results." },
                        { "fetch-record.read", "Retrieve records." },
                    },
                },
            },
        };

        var oauthRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("oauth2_clientCredentials", document)] = [],
        };

        foreach (var (pathKey, pathItem) in document.Paths)
        {
            var route = pathKey.Trim('/').ToLowerInvariant();

            var isPublic =
                route.EndsWith("status")
                || route.EndsWith("health")
                || route.EndsWith("token")
                || route.StartsWith("openapi")
                || route.StartsWith("swagger");

            if (pathItem.Operations == null)
                continue;
            foreach (var op in pathItem.Operations.Values)
            {
                if (isPublic)
                {
                    op.Security = new List<OpenApiSecurityRequirement>();
                    continue;
                }

                op.Security = new List<OpenApiSecurityRequirement> { oauthRequirement };
            }
        }

        document.Tags = new HashSet<OpenApiTag>
        {
            Tag("Health", "Service health check", 1),
            Tag("Auth", "Simulate an OAuth provider", 2),
            Tag("Find", "Query a custodian about records they might hold on a SUI", 10),
            Tag("Fetch", "Get record details from a custodian", 11),
        };

        return Task.CompletedTask;
    }
}
