using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace OpenApi;

public sealed class FindDocumentFilter : IDocumentFilter
{
    public void Apply(IHttpRequestDataObject req, OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes["oauth2_clientCredentials"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri("/v1/auth/token", UriKind.Relative),
                        Scopes = new Dictionary<string, string>
                        {
                            { "find-record.read", "Read search status and results." },
                            { "fetch-record.read", "Retrieve records." }
                        }
                    }
                }
            };

        var oauthRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2_clientCredentials"
                }
            }] = Array.Empty<string>()
        };

        foreach (var (pathKey, pathItem) in document.Paths)
        {
            var route = pathKey.Trim('/').ToLowerInvariant();

            bool isPublic =
                route.EndsWith("status") ||
                route.EndsWith("token") ||
                route.StartsWith("openapi") ||
                route.StartsWith("swagger");

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

        document.Tags = new List<OpenApiTag>
        {
            Tag("Status", "Service health check", 1),
            Tag("Auth", "Simulate an OAuth provider", 2),

            Tag("LOCAL-AUTHORITY-01", "Example Local Authority custodian. GET with standard recordType query.", 10),
            Tag("EDUCATION-01", "Example Education custodian. POST with JSON body.", 11),
            Tag("HEALTH-01", "Example Health custodian. GET with `type` query.", 12),
            Tag("POLICE-01", "Example Police custodian. POST, fixed crime-justice record type.", 13),
            Tag("HOUSING-01", "Example Housing custodian. GET with optional recordType path segment.", 14)
        };
    }

    private static OpenApiTag Tag(string name, string description, int order) =>
        new OpenApiTag
        {
            Name = name,
            Description = description,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-tag-order"] = new OpenApiInteger(order)
            }
        };
}
