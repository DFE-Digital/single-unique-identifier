using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using static SUI.Find.FindApi.Middleware.ResponseTracingMiddleware;

namespace SUI.Find.FindApi.OpenApi;

[ExcludeFromCodeCoverage(Justification = "OpenAPI filters do not contain any logic to be tested.")]
public sealed class FindDocumentFilter : IDocumentFilter
{
    public void Apply(IHttpRequestDataObject req, OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        var dateOnlyKeys = new[] { "dateOnly", "DateOnly" };
        foreach (var key in dateOnlyKeys.Where(key => document.Components.Schemas.ContainsKey(key)))
        {
            var schema = document.Components.Schemas[key];
            schema.Type = "string";
            schema.Format = "date";
            schema.Properties.Clear();
        }

        ConfigureSecuritySchemes(document);

        TransformPaths(document);

        ConfigureTags(document);

        TransformIntEnumsToStrings(document);
    }

    private static void ConfigureSecuritySchemes(OpenApiDocument document)
    {
        var accessTokenUrl = Environment.GetEnvironmentVariable("AccessTokenUrl");

        if (string.IsNullOrWhiteSpace(accessTokenUrl))
        {
            throw new InvalidOperationException("AccessTokenUrl is not set in configuration.");
        }

        document.Components.SecuritySchemes["oauth2_clientCredentials"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                ClientCredentials = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri(accessTokenUrl, UriKind.RelativeOrAbsolute),
                    Scopes = new Dictionary<string, string>
                    {
                        { "match-record.read", "Obtain the id for a person." },
                        { "find-record.read", "Read search status and results." },
                        { "find-record.write", "Create and cancel searches." },
                        { "fetch-record.read", "Retrieve records." },
                        { "fetch-record.write", "Share records." },
                        { "work-item.read", "Query the work item queue" },
                        { "work-item.write", "Update the work item queue" },
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
    }

    private static void TransformPaths(OpenApiDocument document)
    {
        var traceIdHeader = new OpenApiHeader
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

        document.Components.Headers[TraceIdHeaderName] = traceIdHeader;
        document.Components.Headers[InvocationIdHeaderName] = invocationIdHeader;

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

        var oauthRequirementMatch = new OpenApiSecurityRequirement
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
            ] = ["match-record.read"],
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
                        oauthRequirementMatch,
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
                        responseHeaders[TraceIdHeaderName] = traceIdHeader;
                        responseHeaders[InvocationIdHeaderName] = invocationIdHeader;
                    }
                }
            }
        }

        var orderedPaths = document.Paths.OrderBy(p => p.Key).ToArray();

        var newPaths = new OpenApiPaths();
        foreach (var p in orderedPaths)
        {
            p.Value.Operations = p
                .Value.Operations.OrderBy(op =>
                {
                    var httpMethod = op.Key;

                    return httpMethod switch
                    {
                        OperationType.Options => 0,
                        OperationType.Put => 1,
                        OperationType.Post => 2,
                        OperationType.Patch => 3,
                        OperationType.Get => 4,
                        OperationType.Head => 5,
                        OperationType.Delete => 6,
                        OperationType.Trace => 7,
                        _ => 50,
                    };
                })
                .ToDictionary();

            newPaths.Add(p.Key, p.Value);
        }

        document.Paths = newPaths;
    }

    private static void ConfigureTags(OpenApiDocument document)
    {
        document.Tags = new List<OpenApiTag>
        {
            Tag("Health", "Service health check", 1),
            Tag("Auth", "Simulate an OAuth provider", 2),
            Tag("Match", "Locate the id for a person", 3),
            Tag("Searches", "Start, get or cancel a search (Fan-out Architecture)", 4),
            Tag("SearchesV2", "Start or get a search (Polling Architecture)", 5),
            Tag("Work", "Check for and submit results to searches (Polling Architecture)", 6),
            Tag("Fetch", "Fetch records from providers", 7),
        };
    }

    /// <summary>
    /// Searches for and transforms integer enum properties to string representations where applicable in the OpenAPI document.
    /// Ordinarily this would have been done using a Schema Transformer, but the Azure Functions SDK does not support them.
    /// </summary>
    /// <example>
    /// For example, transforms:
    /// <code language="yaml">
    ///   searchResultsV2:
    ///     properties:
    ///       status:
    ///         enum:
    ///           - 0
    ///           - 1
    ///           - 2
    ///           - 3
    ///           - 4
    ///           - 5
    ///           - 6
    ///         type: integer
    ///         format: int32
    ///         default: 0
    /// </code>
    /// to:
    /// <code language="yaml">
    ///   searchResultsV2:
    ///     properties:
    ///       status:
    ///         enum:
    ///           - None
    ///           - Queued
    ///           - Running
    ///           - Completed
    ///           - Failed
    ///           - Cancelled
    ///           - Expired
    ///         type: string
    /// </code>
    /// </example>
    private static void TransformIntEnumsToStrings(OpenApiDocument document)
    {
        // Get all the loaded public types
        var publicTypes = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.ExportedTypes.Where(x => !x.IsAbstract))
            .ToLookup(x => x.Name, StringComparer.OrdinalIgnoreCase);

        // For each schema, check any of the properties which are integer enums and then try and find the
        // corresponding C# enum and convert the schema property to a string representation.
        foreach (var (schemaName, schema) in document.Components.Schemas)
        {
            foreach (
                var (schemaPropertyName, schemaEnumProperty) in schema.Properties.Where(p =>
                    p.Value.Type == "integer" && p.Value.Enum.Any()
                )
            )
            {
                var modelProperty = publicTypes[schemaName]
                    .SelectMany(t =>
                        t.GetProperties()
                            .Where(p =>
                                p.Name.Equals(
                                    schemaPropertyName,
                                    StringComparison.OrdinalIgnoreCase
                                )
                                && p.PropertyType.IsEnum
                                && p.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType
                                    == typeof(JsonStringEnumConverter)
                            )
                    )
                    .FirstOrDefault();

                if (modelProperty != null)
                {
                    schemaEnumProperty.Type = "string";
                    schemaEnumProperty.Enum.Clear();
                    schemaEnumProperty.Format = null;
                    schemaEnumProperty.Default = null;
                    var names = Enum.GetNames(modelProperty.PropertyType);
                    foreach (var name in names)
                    {
                        schemaEnumProperty.Enum.Add(new OpenApiString(name));
                    }
                }
            }
        }
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
