using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API
{
    [ExcludeFromCodeCoverage]
    public class CustodiansOpenApiSchemaTransformer : IOpenApiSchemaTransformer
    {
        private readonly List<XDocument> _xmlDocs = new();

        public CustodiansOpenApiSchemaTransformer(IWebHostEnvironment env)
        {
            // Load XML docs for API + dependent projects
            var assemblies = new[]
            {
                Assembly.GetEntryAssembly()!,
                typeof(SUI.Custodians.Domain.Models.CrimeDataRecordV1).Assembly,
                typeof(SUI.StubCustodians.Application.Models.RecordEnvelope<>).Assembly,
            };

            foreach (var asm in assemblies)
            {
                var xmlPath = Path.Combine(
                    env.ContentRootPath,
                    "bin",
                    "Debug",
                    "net9.0",
                    $"{asm.GetName().Name}.xml"
                );
                if (File.Exists(xmlPath))
                    _xmlDocs.Add(XDocument.Load(xmlPath));
            }
        }

        public Task TransformAsync(
            OpenApiSchema schema,
            OpenApiSchemaTransformerContext context,
            CancellationToken cancellationToken
        )
        {
            if (context.JsonTypeInfo?.Type == null)
            {
                return Task.CompletedTask;
            }

            ApplyXmlToSchema(schema, context.JsonTypeInfo.Type, new HashSet<Type>());

            return Task.CompletedTask;
        }

        private void ApplyXmlToSchema(OpenApiSchema schema, Type type, HashSet<Type> visited)
        {
            if (visited.Contains(type))
            {
                return;
            }

            visited.Add(type);

            // Handle generic wrappers (like RecordEnvelope<T>)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RecordEnvelope<>))
            {
                var payloadProp = type.GetProperty("Payload");
                if (
                    payloadProp != null
                    && schema.Properties.TryGetValue("payload", out var payloadSchema)
                )
                {
                    // Keep payload fully expanded
                    payloadSchema.Description ??= GetXmlSummary(payloadProp);
                    payloadSchema.Example ??= GetXmlExample(payloadProp);
                    ApplyXmlToSchema(payloadSchema, payloadProp.PropertyType, visited);
                }

                // Set schemaUri as string reference
                if (schema.Properties.TryGetValue("schemaUri", out var schemaUriSchema))
                {
                    var payloadType = type.GetGenericArguments()[0];
                    schemaUriSchema.Description ??= "URI of the payload schema";
                    schemaUriSchema.Example = new OpenApiString(
                        $"#/components/schemas/{payloadType.Name}"
                    );
                }
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!schema.Properties.TryGetValue(ToCamelCase(prop.Name), out var propSchema))
                    continue;

                propSchema.Description ??= GetXmlSummary(prop);
                propSchema.Example ??= GetXmlExample(prop);

                // Recursively handle nested types
                if (!IsSimpleType(prop.PropertyType))
                {
                    if (
                        typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType)
                        && prop.PropertyType.IsGenericType
                    )
                    {
                        var itemType = prop.PropertyType.GetGenericArguments()[0];
                        if (propSchema.Items != null)
                            ApplyXmlToSchema(propSchema.Items, itemType, visited);
                    }
                    else
                    {
                        ApplyXmlToSchema(propSchema, prop.PropertyType, visited);
                    }
                }
            }
        }

        private static string ToCamelCase(string name) =>
            char.ToLowerInvariant(name[0]) + name.Substring(1);

        private static bool IsSimpleType(Type type) =>
            type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime);

        private string? GetXmlSummary(MemberInfo member)
        {
            var memberName = member switch
            {
                Type t => $"T:{t.FullName}",
                PropertyInfo p => $"P:{p.DeclaringType!.FullName}.{p.Name}",
                _ => null,
            };

            if (memberName == null)
                return null;

            foreach (var doc in _xmlDocs)
            {
                var element = doc.Descendants("member")
                    .FirstOrDefault(x => (string?)x.Attribute("name") == memberName);
                var summary = element?.Element("summary")?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(summary))
                    return summary;
            }

            return null;
        }

        private OpenApiString? GetXmlExample(PropertyInfo prop)
        {
            var memberName = $"P:{prop.DeclaringType!.FullName}.{prop.Name}";

            foreach (var doc in _xmlDocs)
            {
                var element = doc.Descendants("member")
                    .FirstOrDefault(x => (string?)x.Attribute("name") == memberName);

                var example = element?.Element("example")?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(example))
                    return new OpenApiString(example);
            }

            return null;
        }
    }
}
