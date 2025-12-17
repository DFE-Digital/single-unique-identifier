using System.Diagnostics.CodeAnalysis;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SUI.StubCustodians.API.OpenApiTransformers
{
    [ExcludeFromCodeCoverage]
    public class CustodiansOpenApiDocumentTransformer(IApiVersionDescriptionProvider provider)
        : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken
        )
        {
            // Find matching API version description
            var apiVersionDescription = provider.ApiVersionDescriptions.FirstOrDefault(d =>
                d.GroupName.Equals(context.DocumentName, StringComparison.OrdinalIgnoreCase)
            );

            if (apiVersionDescription != null)
            {
                document.Info ??= new OpenApiInfo();
                document.Info.Title = "Custodians API";
                document.Info.Version = apiVersionDescription.ApiVersion.ToString();

                if (apiVersionDescription.IsDeprecated)
                {
                    document.Info.Description =
                        "This API version is DEPRECATED. Please use one of the new APIs available from the explorer.";
                }
            }

            return Task.CompletedTask;
        }
    }
}
