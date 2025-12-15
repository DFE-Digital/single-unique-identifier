using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SUI.StubCustodians.API.OpenApiTransformers;

public class DerivedTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var type = context.JsonTypeInfo.Type;

        if (type.FullName.Contains("SuiRecord"))
        {
            var record = type;
        }
        // Check if the current type has JsonDerivedType attributes
        var derivedAttributes = type.GetCustomAttributes<JsonDerivedTypeAttribute>().ToArray();

        if (derivedAttributes.Any())
        {
            // Initialize the OneOf list
            schema.OneOf = new List<OpenApiSchema>();

            foreach (var attr in derivedAttributes)
            {
                // 1. Determine the Schema ID for the derived type
                // You might need to customize this ID generation to match your project's convention
                var derivedTypeId = attr.DerivedType.Name;

                // 2. Add a reference to the derived type in the 'OneOf' list
                schema.OneOf.Add(
                    new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Id = derivedTypeId,
                            Type = ReferenceType.Schema,
                        },
                    }
                );
            }

            // IMPORTANT: The generator might not have "seen" the derived types yet
            // if they aren't used directly in an endpoint.
            // This relies on the generator discovering them recursively or you adding them manually.
        }

        return Task.CompletedTask;
    }
}
