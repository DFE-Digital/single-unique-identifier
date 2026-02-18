using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace SUI.Find.FindApi.OpenApi;

[ExcludeFromCodeCoverage(Justification = "OpenAPI filters do not contain any logic to be tested.")]
public sealed class FindDocumentFilter : IDocumentFilter
{
    public void Apply(IHttpRequestDataObject req, OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

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
                        { "match-record.read", "Obtain the id for a person." },
                        { "find-record.read", "Read search status and results." },
                        { "find-record.write", "Create and cancel searches." },
                        { "fetch-record.read", "Retrieve records." },
                        { "fetch-record.write", "Share records." },
                    },
                },
            },
        };

        document.Components.SecuritySchemes["x-api-key"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "x-api-key",
            Description = "API Key for authentication",
        };

        var operationIdHeader = new OpenApiHeader
        {
            Description = "Primary trace ID for the whole operation",
            Schema = new OpenApiSchema { Type = "string" },
            Example = new OpenApiString("082d58852e3244eed72f52f41e0f8045"),
        };

        var invocationIdHeader = new OpenApiHeader
        {
            Description = "Secondary trace ID for the whole operation",
            Schema = new OpenApiSchema { Type = "string" },
            Example = new OpenApiString("a49d73e8-dc36-4be5-92a6-9bf62868aa99"),
        };

        document.Components.Headers["Operation-Id"] = operationIdHeader;
        document.Components.Headers["Invocation-Id"] = invocationIdHeader;

        var oauthRequirement = new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "oauth2_clientCredentials",
                    },
                }
            ] = Array.Empty<string>(),
        };

        var apiKeyRequirement = new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "x-api-key",
                    },
                }
            ] = Array.Empty<string>(),
        };

        foreach (var (pathKey, pathItem) in document.Paths)
        {
            var route = pathKey.Trim('/').ToLowerInvariant();

            bool isPublic =
                route.EndsWith("status")
                || route.EndsWith("token")
                || route.StartsWith("openapi")
                || route.StartsWith("swagger");

            bool isMatchEndpoint = route.Contains("matchperson");

            var hasTraceHeaders = !(
                route.EndsWith("health")
                || route.StartsWith("openapi")
                || route.StartsWith("swagger")
            );

            foreach (var op in pathItem.Operations.Values)
            {
                if (isPublic)
                {
                    op.Security = new List<OpenApiSecurityRequirement>();
                    continue;
                }

                if (isMatchEndpoint)
                {
                    op.Security = new List<OpenApiSecurityRequirement>
                    {
                        oauthRequirement,
                        apiKeyRequirement,
                    };
                }
                else
                {
                    op.Security = new List<OpenApiSecurityRequirement> { oauthRequirement };
                }

                if (hasTraceHeaders)
                {
                    foreach (var responseHeaders in op.Responses.Values.Select(x => x.Headers))
                    {
                        responseHeaders["Operation-Id"] = operationIdHeader;
                        responseHeaders["Invocation-Id"] = invocationIdHeader;
                    }
                }
            }
        }

        string[] findOrder =
        {
            "/api/v1/searches",
            "/api/v1/searches/{jobId}",
            "/api/v1/searches/{jobId}/results",
        };

        var orderedPaths = document
            .Paths.OrderBy(p =>
            {
                var key = p.Key.ToLowerInvariant();

                if (key == "/health")
                    return 0;
                if (key == "/api/v1/auth/token")
                    return 1;

                int findIndex = Array.FindIndex(
                    findOrder,
                    o => o.Equals(key, StringComparison.OrdinalIgnoreCase)
                );

                if (findIndex >= 0)
                    return 10 + findIndex;
                if (key.StartsWith("/api/v1/records"))
                    return 20;

                return 999;
            })
            .ToList();

        var newPaths = new OpenApiPaths();
        foreach (var p in orderedPaths)
        {
            newPaths.Add(p.Key, p.Value);
        }

        document.Paths = newPaths;

        document.Tags = new List<OpenApiTag>
        {
            Tag("Health", "Service health check", 1),
            Tag("Auth", "Simulate an OAuth provider", 2),
            Tag("Match", "Locate the id for a person", 3),
            Tag("Find", "Start, get or cancel a search", 4),
            Tag("Fetch", "Fetch records from providers", 5),
        };
    }

    private static OpenApiTag Tag(string name, string description, int order) =>
        new()
        {
            Name = name,
            Description = description,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-tag-order"] = new OpenApiInteger(order),
            },
        };
}
