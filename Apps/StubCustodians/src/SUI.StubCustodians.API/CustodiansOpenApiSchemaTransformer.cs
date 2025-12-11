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
        private static readonly string LogOutputPath;

        static CustodiansOpenApiSchemaTransformer()
        {
            LogOutputPath = Path.Combine(
                Path.GetDirectoryName(typeof(CustodiansOpenApiSchemaTransformer).Assembly.Location)
                    ?? "",
                $"{nameof(CustodiansOpenApiSchemaTransformer)}.log"
            );
            using var _ = File.Create(LogOutputPath);
        }

        private static void Log(string message)
        {
#if DEBUG
            File.AppendAllLines(LogOutputPath, [message]);
#endif
        }

        private readonly Dictionary<Assembly, XDocument> _xmlDocs = [];

        public Task TransformAsync(
            OpenApiSchema schema,
            OpenApiSchemaTransformerContext context,
            CancellationToken cancellationToken
        )
        {
            Log(
                $"TransformAsync: {context.DocumentName} - {context.JsonPropertyInfo?.DeclaringType} {context.JsonPropertyInfo?.Name}"
            );

            var declaringType = context.JsonPropertyInfo?.DeclaringType;

            // Handle `SchemaUri` special case, so that we specify to implementors what the correct SchemaUri is for each record type.
            if (
                declaringType is { IsGenericType: true }
                && declaringType.GetGenericTypeDefinition() == typeof(RecordEnvelope<>)
            )
            {
                if (
                    context.JsonPropertyInfo?.Name
                    == ToCamelCase(nameof(RecordEnvelope<object>.SchemaUri))
                )
                {
                    var payloadType = declaringType.GetGenericArguments()[0];
                    schema.Description ??= $"URI of the {payloadType.Name} payload schema";
                    schema.Example = new OpenApiString(
                        $"https://schemas.example.gov.uk/sui/{payloadType.Name}"
                    );
                }
            }

            // Pull in descriptions and examples from XML documentation comments
            // Note that the JSON schema doesn't allow anything at the same level of $ref, and the correct way is to provide the description in the referenced object.
            // See: https://datatracker.ietf.org/doc/html/draft-pbryan-zyp-json-ref-03#section-3
            // The MS OpenAPI library obeys this rule, however if different descriptions are provided the library creates duplicate schemas.
            // So, for references, we must provide the same generic description.
            var isRef =
                context.JsonTypeInfo is { Type.IsValueType: false }
                && context.JsonTypeInfo.Type != typeof(string)
                && schema.Type != "array";

            if (isRef)
            {
                schema.Description ??= GetXmlSummary(context.JsonTypeInfo.Type);
            }
            else
            {
                var prop = context.JsonPropertyInfo?.DeclaringType.GetProperty(
                    context.JsonPropertyInfo.Name,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
                );

                if (prop != null)
                {
                    schema.Description ??= GetXmlSummary(prop);
                    schema.Example ??= GetXmlExample(prop);
                }
            }

            return Task.CompletedTask;
        }

        private static string ToCamelCase(string name) =>
            char.ToLowerInvariant(name[0]) + name[1..];

        private XDocument? GetXmlDoc(Assembly assembly)
        {
            if (_xmlDocs.TryGetValue(assembly, out var xmlDocs))
                return xmlDocs;

            var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
            if (assemblyDirectory == null)
                return null;

            var xmlPath = Path.Combine(assemblyDirectory, $"{assembly.GetName().Name}.xml");
            return File.Exists(xmlPath) ? _xmlDocs[assembly] = XDocument.Load(xmlPath) : null;
        }

        private string? GetXmlDocValue(MemberInfo member, string elementName)
        {
            var (memberName, assembly) = member switch
            {
                Type t => ($"T:{t.FullName}", t.Assembly),
                PropertyInfo p => (
                    $"P:{p.DeclaringType?.FullName}.{p.Name}",
                    p.DeclaringType?.Assembly
                ),
                _ => (null, null),
            };

            if (memberName == null || assembly == null)
                return null;

            var xmlDoc = GetXmlDoc(assembly);

            var element = xmlDoc
                ?.Descendants("member")
                .FirstOrDefault(x => (string?)x.Attribute("name") == memberName);
            var summary = element?.Element(elementName)?.Value?.Trim();
            return !string.IsNullOrWhiteSpace(summary) ? summary : null;
        }

        private string? GetXmlSummary(MemberInfo member) => GetXmlDocValue(member, "summary");

        private OpenApiString? GetXmlExample(PropertyInfo prop)
        {
            var example = GetXmlDocValue(prop, "example");
            return !string.IsNullOrWhiteSpace(example) ? new OpenApiString(example) : null;
        }
    }
}
