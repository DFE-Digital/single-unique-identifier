using System.Diagnostics.CodeAnalysis;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SUI.StubCustodians.API
{
    [ExcludeFromCodeCoverage]
    public class ConfigureOpenApiOptions : IOpenApiDocumentTransformer
    {
        private readonly IApiVersionDescriptionProvider _provider;

        public ConfigureOpenApiOptions(IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
        }

        public Task TransformAsync(
            OpenApiDocument document,
            OpenApiDocumentTransformerContext context,
            CancellationToken cancellationToken
        )
        {
            // Find matching API version description
            var apiVersionDescription = _provider.ApiVersionDescriptions.FirstOrDefault(d =>
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
